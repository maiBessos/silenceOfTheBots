using GoE.GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Policies;
using System.Drawing;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.AdvRouting;
using GoE.Utils.Algorithms;
using GoE.Utils;

namespace GoE.AdvRouting
{
    class AdvRoutingGameProcess : AGameProcess
    {
        public PointF sink;
        GoE.Utils.PointDataStructs.PointFSet allRouters;
        public Dictionary<PointF,int> routerPointToIdx { get; protected set; }
        public List<PointF> routerIdxToPoint { get; protected set; }
        public List<int> transmittingRouters { get; protected set; }
        public PointF InitialDetectedRouter { get; protected set; }

        public AdvRoutingGameParams Params { get; protected set; } // all game constants
        public int GameStep { get; set; }
        
        
        //public CommunicationGraph O_c { get; protected set; } // evaders observed by pursuers (due evader communicating)
        public List<PointF> o_C;
        
        public int CapturedEvaders { get { return EliminatedRouters.Count; } }
        //public HashSet<GoE.GameLogic.Utils.CapturedObservation> O_d { get; protected set; }
        public List<PointF> EliminatedRouters
        {
            get;protected set;
        }

        public int currentRound
        {
            get; protected set;
        }

        /// <summary>
        /// heuristic to estimate for total reward, if game was finished prematurely. 
        /// </summary>
        public double MinDistanceToSink
        {
            get;
            protected set;
        }

        /// <summary>
        /// heuristic to estimate for total reward, if game was finished prematurely. 
        /// for each interceptor visitation, this sums the distance from visitation location to the sink
        /// </summary>
        public double TotalDistancesToSink
        {
            get;protected set;
        }

        /// <summary>
        /// distance between initiallly exposed router and sink
        /// </summary>
        public double InitialDistanceToSink
        {
            get;protected set;
        }

        public double transferredData = 0;
        public double GameResultReward
        {
            get
            {
                return transferredData;
            }
        }
        
        public bool IsFinished
        {
            get; protected set;
        }

        public IGameProcessGUI constructGUI()
        {
            return new frmAdvRoutingGameProcessView();
        }

        public IGameParams constructGameParams()
        {
            return new AdvRoutingGameParams();
        }

        public void finishGame()
        {
            IsFinished = true;
        }

        AdvRoutingPursuersPolicyBase pursuersAlg;
        AdvRoutingRouterPolicyBase routersAlg;
        public void init(APursuersPolicy pursuerJointPolicy, IEvadersPolicy evadersJointPolicy)
        {
            TotalDistancesToSink = 0;
            MinDistanceToSink = double.MaxValue;
            currentRound = 0;
            IsFinished = false;
            EliminatedRouters = new List<PointF>();
            rand = new ThreadSafeRandom();
            routersAlg = (AdvRoutingRouterPolicyBase)evadersJointPolicy;
            pursuersAlg = (AdvRoutingPursuersPolicyBase)pursuerJointPolicy;
            pursuerTurn = false;

            routerIdxToPoint = routersAlg.getInitialNetwork();
            sink = routersAlg.sink;
            var allRoutersSet = new HashSet<PointF>();
            routerPointToIdx = new Dictionary<PointF, int>();
            o_C = new List<PointF>();

            if (routerIdxToPoint.Count == 0)
            {
                finishGame();
                InitialDistanceToSink = double.MaxValue;
                TotalDistancesToSink = double.MaxValue;
                transferredData = 0;
                return;
            }

            for (int r = 0; r < routerIdxToPoint.Count; ++r)
            {
                routerPointToIdx[routerIdxToPoint[r]] = r;
                allRoutersSet.Add(routerIdxToPoint[r]);
            }
            allRouters = new GoE.Utils.PointDataStructs.PointFSet(routerIdxToPoint);
            

            HashSet<PointF> connectedToSink = new HashSet<PointF>();
            checkConnectivityRec(sink, allRoutersSet, connectedToSink);
            connectedToSink.Remove(sink);

            if (Params.singleSourceRouter)
                InitialDetectedRouter = routersAlg.SourceRouter;
            else
                InitialDetectedRouter = connectedToSink.ToList().chooseRandomItem(rand.rand);//routerIdxToPoint[rand.Next(0, routerPointToIdx.Count)];
            InitialDistanceToSink = InitialDetectedRouter.subtruct(sink).distance(new PointF(0, 0));

            pursuersAlg.setInitialDetectedRouter(InitialDetectedRouter);
        }
        
       
        /// <summary>
        /// tells how many transmitting routers are connected to the node
        /// </summary>
        /// <returns></returns>
        private int checkConnectivityRec(PointF node, HashSet<PointF> transmittingRouters, HashSet<PointF> alreadyVisited)
        {
            alreadyVisited.Add(node);

            int res = 1;
            var adjacentPoints = allRouters.findNearest(node, 1);
            foreach (var p in adjacentPoints)
            {
                if (alreadyVisited.Contains(p))
                    continue;
                if (!transmittingRouters.Contains(p))
                {
                    alreadyVisited.Add(p);
                    continue;
                }
                res += checkConnectivityRec(p, transmittingRouters, alreadyVisited);
            }
            return res;
        }

        public void initParams(IGameParams param, AGameGraph GameGraph)
        {
            graph = (EmptyEnvironment)GameGraph;
            this.Params = (AdvRoutingGameParams)param;
        }

        public PointF pursuerLoc { get; protected set; }

        public const string MinDistanceToSinkHName = "MinDistanceToSink";
        public const string AvgDistancesToSinkHName = "AvgDistancesToSink";
        public IDictionary<string, string> ResultValues
        {
            get
            {
                var res = new Dictionary<string, string>();
                //FIXME: make constants for these heuristics. we also receive input parameter that regards the same heuristics, but has different names. this is a disaster
                res[MinDistanceToSinkHName] = ((float)MinDistanceToSink / (float)InitialDistanceToSink).ToString();
                res[AvgDistancesToSinkHName] = ((float)TotalDistancesToSink / ( Math.Max(1,currentRound) * (float)InitialDistanceToSink)).ToString();
                
                return res;
            }
        }

        private int latestAddedReward = 0;

        public bool invokeNextPolicy()
        {
            return Exceptions.ConditionalTryCatch<Exception,bool>(false,
                () => { return invokeNextPolicyInner(); },
                (Exception ex) =>
                {
                    AppSettings.handleGameException(ex);
                });
        }
        public bool invokeNextPolicyInner()
        {
            
            if(pursuerTurn)
            {
                var pursuerAction =  pursuersAlg.getNextLocation(o_C);
                pursuerLoc = pursuerAction.Item1;
                o_C.Clear();

                double dist = pursuerLoc.distance(sink);
                TotalDistancesToSink += dist;
                MinDistanceToSink = Math.Min(MinDistanceToSink, dist);
                if (Params.accurateInterception)
                {
                    foreach (var p in transmittingRouters)
                        if (pursuerLoc.distance2F(routerIdxToPoint[p]) <= 1.001)
                            o_C.Add(routerIdxToPoint[p]);

                    if (o_C.Contains(sink))
                    {
                        finishGame();
                        return false; // game ends
                    }
                }
                else
                {
                    // we must check sink separately, since we 'break' the loop after any transmission detection (i.e. sink and another router may be detected simultenaously):
                    if(latestAddedReward > 0 /*make sure sink is detectable*/ && pursuerLoc.distance2F(sink) <=1)
                    {
                        finishGame();
                        return false; // game ends
                    }
                    foreach (var p in transmittingRouters)
                        if (pursuerLoc.distance2F(routerIdxToPoint[p]) <= 1.001)
                        {
                            o_C.Add(pursuerLoc);
                            break;
                        }
                }

                ++currentRound;
            }
            else
            {
                
                transmittingRouters = routersAlg.nextTransmittingPoints(null, out latestAddedReward);

                if(Params.forceContinuousTransmission && latestAddedReward == 0)
                {
                    // routers cheated! end game 
                    transferredData = 0;
                    finishGame();
                    return false;
                }
                if (Params.singleSourceRouter)
                    latestAddedReward = Math.Min(latestAddedReward,1);

                transferredData += latestAddedReward;

#if DEBUG
                HashSet<int> uniqueIndicesChecker = new HashSet<int>(transmittingRouters);
                if(uniqueIndicesChecker.Count != transmittingRouters.Count)
                {
                    throw new Exception("Sanity Check failed: dupe transmitting points");
                }

                var transmittingRoutersPoints = new HashSet<PointF>();
                foreach (var tp in transmittingRouters)
                    transmittingRoutersPoints.Add(routerIdxToPoint[tp]);
                if ((checkConnectivityRec(sink, transmittingRoutersPoints, new HashSet<PointF>()) - 1) < latestAddedReward)
                {
                    throw new Exception("Sanity Check failed: reward mismatch");
                }
#endif
            }

            pursuerTurn = !pursuerTurn;

            
            return true;
        }

        public string getGraphTypename()
        {
            return GoE.Utils.ReflectionUtils.GetObjStaticTypeName(graph);

        }

        public Type getRouterPolicyBaseType()
        {
            return typeof(AdvRoutingRouterPolicyBase);
        }

        public Type getPursuerPolicyBaseType()
        {
            return typeof(AdvRoutingPursuersPolicyBase);
        }
        
        private bool pursuerTurn;
        private EmptyEnvironment graph;
        private AdvRoutingPursuersPolicyBase Pi_p;
        private AFrontsGridRoutingEvadersPolicy Pi_e;
        private double transmissionDetectionProb;
        private ThreadSafeRandom rand;
        
    }
}
