using GoE.AppConstants;
using GoE.GameLogic;
using GoE.UI;
using GoE.Utils.Algorithms;
using GoE.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoE.Policies
{
    /// <summary>
    /// manages Sink and SourcePoints automatically
    /// </summary>
    public class CommunicationGraph
    {
        public delegate double UtilityGetter(Point node); // for a node, tells how desired it's inclusion is

        private Dictionary<Point, List<Point>> backwardEdges;
        private Dictionary<Point, List<Point>> forwardEdges; // each point specifies all points with >=x, that are within distance r_e
        public static Point SourcePoint { get { return new Point(-1, -1); } } // the first graph node(outside game graph), that sends data to all evaders

        private static List<Point> sinkDummy = new List<Point>();
        public static List<Point> Sink { get { return sinkDummy; } } // if a point is connected to this, then it can transmit data to the sink

        /// <summary>
        /// creates a graph with given nodes, where nodes with distance <=maxConnectionDist are connected (edge goes from smaller X to larger X)
        /// </summary>
        /// <param name="nodes">
        /// will be sorted by x axis
        /// </param>
        /// <param name="maxConnectionDist"></param>
        /// <param name="maxSourceXDist">
        /// addPoint(nodes[i], maxConnectionDist, maxConnectionDist) will be used
        /// </param>
        /// <param name="minSinkXDist">
        /// addPoint(nodes[i], maxConnectionDist, maxConnectionDist) will be used
        /// </param>
        public CommunicationGraph(ref List<Point> nodes, int maxConnectionDist, int maxSourceXDist, int minSinkXDist)
        {
            backwardEdges = new Dictionary<Point, List<Point>>();
            forwardEdges = new Dictionary<Point, List<Point>>();
            forwardEdges[SourcePoint] = new List<Point>();
            backwardEdges[SourcePoint] = new List<Point>();


            nodes.Sort(new Comparison<Point>((Point lhs, Point rhs)=> { return lhs.X.CompareTo(rhs.X); }));
            for (int i = 0; i < nodes.Count; ++i)
                if(nodes[i] != SourcePoint)
                    addPoint(nodes[i], maxSourceXDist, minSinkXDist);

            for (int i = 0; i < nodes.Count; ++i)
                for (int j=i+1; j < nodes.Count && nodes[i].X <= nodes[j].X + maxConnectionDist; ++j)
                    if(nodes[i].manDist(nodes[j]) <= maxConnectionDist)
                        addEdge(nodes[i], nodes[j]);
        }
        public CommunicationGraph()
        {
            backwardEdges = new Dictionary<Point, List<Point>>();
            forwardEdges = new Dictionary<Point, List<Point>>();
            forwardEdges[SourcePoint] = new List<Point>();
            backwardEdges[SourcePoint] = new List<Point>();
        }

        ///// <summary>
        ///// returns a graph that contains minimalPath, but also up to 'maxNodesToAdd' additional nodes (preferrably with highest values).
        ///// Each additional nod must connect at least two nodes that are already 
        ///// </summary>
        ///// <param name="minimalPath">
        ///// starts with 'SourcePoint' , and ends with a node that may transmit into sink
        ///// </param>
        ///// <param name="maxNodesToAdd"></param>
        ///// <param name="evaluator"></param>
        ///// <returns></returns>
        //public CommunicationGraph getReduandantComGraph(List<Point> minimalPath, int maxNodesToAdd, UtilityGetter evaluator)
        //{
        //    CommunicationGraph res = new CommunicationGraph();

        //    return res;
        //}

        /// <summary>
        /// includes SourcePoint
        /// </summary>
        /// <returns></returns>
        public List<Point> getAllTransmittingPoints()
        {
            var res = forwardEdges.Keys.ToList();
            //res.Remove(SourcePoint);
            return res;
        }
        /// <summary>
        /// if p.X <= maxSourceXDist, source will connect to p
        /// if p.X >= minSinkXDist, p will connect to sink
        /// </summary>
        /// <param name="p"></param>
        /// <param name="maxSourceXDist"></param>
        /// <param name="maxSinkXDist"></param>
        public void addPoint(Point p, int maxSourceXDist, int minSinkXDist)
        {
            backwardEdges[p] = new List<Point>();
            if (p.X >= minSinkXDist)
            {
                forwardEdges[p] = Sink;
            }
            else
            {
                forwardEdges[p] = new List<Point>();
                if (p.X <= maxSourceXDist)
                {

#if DEBUG
                    if (forwardEdges[SourcePoint].Contains(p) || SourcePoint.X > p.X || SourcePoint == p)
                    {
                        int a = 0;
                    }
#endif

                    backwardEdges[p].Add(SourcePoint);
                    forwardEdges[SourcePoint].Add(p);
                    
                    
                }
            }
            
        }
        public void addEdge(Point from, Point to)
        {
            if(forwardEdges[from] != Sink)
                forwardEdges[from].Add(to);
            else
            {
                int a = 0;
            }
            backwardEdges[to].Add(from);
        }
        public bool isConnectedToSink(Point p)
        {
            return forwardEdges[p] == Sink;
        }
        public void removePoint(Point p, out List<Point> backwardConnections, out List<Point> forwardConnections)
        {
            backwardConnections = backwardEdges[p];
            forwardConnections = forwardEdges[p];

            foreach (var prev in backwardEdges[p])
                forwardEdges[prev].Remove(p);

            if(forwardEdges[p] != Sink)
                foreach (var forw in forwardEdges[p])
                {
                    backwardEdges[forw].Remove(p);
                }
        }

        /// <summary>
        /// </summary>
        /// <param name="of"></param>
        /// <param name="sinkMarker"></param>
        /// <returns>
        /// if "of" is connected to sink, returns "sinkMarker"
        /// otherwise, getForwardEdges(of)
        /// </returns>
        public List<Point> getForwardEdgesOrDest(Point of, Point sinkMarker)
        {
            var l = forwardEdges[of];
            if (l == Sink)
            {
                var res = new List<Point>(1);
                res.Add(sinkMarker);
                return res;
            }
            return l;
            
        }

        public List<Point> getBackwardEdges(Point of)
        {
            return backwardEdges[of];
        }
        public List<Point> getForwardEdges(Point of)
        {
            return forwardEdges[of];
        }

        /// <summary>
        /// including "source", excluding sink, excludes points with no connections
        /// </summary>
        public int ConnectedPointsCount
        {
            get { return forwardEdges.Keys.Count; }
        }

    }
    public abstract class AFrontsGridRoutingEvadersPolicy : GoE.Utils.ReflectionUtils.DerivedTypesProvider<AFrontsGridRoutingEvadersPolicy>, IEvadersPolicy
    {
        /// <summary>
        /// it turned 'true' by the policy, 
        /// some performance estimators will stop (useful when we want to calculate leaked data for only the captured evaders)
        /// </summary>
        public bool GaveUp { get { return _gaveUp; } protected set { _gaveUp = value; } }

        public virtual List<ArgEntry> policyInputKeys
        {
            get
            {
                return new List<ArgEntry>();
            }
        }

        private bool _gaveUp = false;
        protected void GiveUp() { _gaveUp = true; }
        /// <summary>
        /// first method called at the begining of the round
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="O_d">
        /// all dead evaders communicate to other evaders that they just got captured + all of their held
        /// observations until that point
        /// </param>
        /// <param name="s"></param>
        public abstract void setGameState(int currentRound, 
            IEnumerable<GoE.GameLogic.Utils.CapturedObservation> O_d,
            AgentGrid<Evader> currentEvaders,
            float maxEvadersToPlace,
            List<Point> currentPatrollerLocations);

        /// <summary>
        /// tells from where transmissions were done (called before getNextStep() )
        /// </summary>
        /// <param name="communicatedObservations"></param>
        public abstract CommunicationGraph communicate();

       /// <summary>
       /// all evader locations, including locations of NEW evaders (maximum new evaders is 'maxEvadersToPlace' as 
       /// specified in the last setGameState() call )
       /// </summary>
       /// <returns></returns>
        public abstract List<Point> getNextStep();

        public bool init(AGameGraph G, IGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            return init(G, (FrontsGridRoutingGameParams)prm, initializedPursuers, pgui, policyParams);
        }
        public abstract bool init(AGameGraph G, FrontsGridRoutingGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null);
        
        /// <summary>
        /// allows policy to flush additional data to the log, after the game is done
        /// </summary>
        public virtual void gameFinished() { }

        /// <summary>
        /// may be called after init()
        /// </summary>
        /// <returns></returns>
        public virtual GameResult getMinLeakedDataTheoreticalBound() { return null; }

        
        public void addGUIMarks(IPolicyGUIInputProvider ui, 
                         GridGameGraph gr,
                         FrontsGridRoutingGameParams prm,
                         Dictionary<Evader, Point> currentEvaderLocations)
        {
            if (ui.hasBoardGUI())
            {
                Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();

                //List<Point> tmpPointList = new List<Point>();
                //foreach (var p in O_p)
                //    tmpPointList.AddRange(p.observedPursuerPath);
                //markedLocations.Add("Pursuer trail(O_p)", tmpPointList);

                //tmpPointList = new List<Point>();
                //foreach (var p in O_p)
                //    tmpPointList.Add(p.observedPursuerPath.Last());
                //markedLocations.Add("Pursuer last pos", tmpPointList);

                //List<Point> detectionArea = new List<Point>();
                //foreach (Evader e in s.ActiveEvaders)
                //{
                //    if (currentEvaderLocations.ContainsKey(e))
                //        detectionArea.AddRange(
                //            gr.getNodesWithinDistance(currentEvaderLocations[e], prm.r_es));
                //}
                //markedLocations.Add("Area within r_s(evaders detecting pursuers)", detectionArea);

                ui.markLocations(markedLocations.toPointFMarkings());
            }
        }
    }
    //class RoutingEvadersPolicyEvadersPolicyUI : ARoutingEvadersPolicy
    //{
    //    private GridGameGraph g;
    //    private RoutingGameParams gm;
    //    private IPolicyGUIInputProvider pgui;

    //    private IEnumerable<GoE.GameLogic.Utils.CapturedObservation> prevO_d;
    //    private List<GameLogic.Utils.PursuerPathObservation> prevO_p;
    //    //private Dictionary<Evader, Location> currentEvadersLocations = new Dictionary<Evader, Location>();
    //    List<Point> evaderPoints = new List<Point>();
    //    private Point RoutingAreaCenter;

    //    private Dictionary<Evader, List<GameLogic.Utils.PursuerPathObservation>> accumObservationsPerEve;
    //    private List<Evader> nextEvadersToCommunicate;


    //    public override bool init(GridGameGraph G, RoutingGameParams prmi, IPursuersPolicy initializedPursuers, IPolicyGUIInputProvider pGui, Dictionary<string, string> policyParams = null)
    //    {
    //        RoutingGameParams prm = prmi;

    //        this.accumObservationsPerEve = new Dictionary<Evader, List<GameLogic.Utils.PursuerPathObservation>>();
    //        foreach (var e in prm.A_E)
    //            this.accumObservationsPerEve[e] = new List<GameLogic.Utils.PursuerPathObservation>();

    //        this.nextEvadersToCommunicate = new List<Evader>();
    //        this.g = G;
    //        this.gm = prm;
    //        this.pgui = pGui;
    //        evaderPoints = new List<Point>();

    //        RoutingAreaCenter = g.getNodesByType(NodeType.Target).First();
    //        return true;
    //    }
    //    public RoutingEvadersPolicyEvadersPolicyUI() { }


    //    public override void setGameState(int currentRound,
    //        IEnumerable<GoE.GameLogic.Utils.CapturedObservation> O_d,
    //        float maxEvadersToPlace)
    //    {
    //        prevO_d = O_d;

    //        foreach (var obs in O_d)
    //            if (evaderPoints.Contains(obs.where))
    //                evaderPoints.Remove(obs.where);

    //        Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();

    //        List<Point> tmpPointList = new List<Point>();

    //        foreach (var p in O_d)
    //            tmpPointList.Add(p.where);
    //        markedLocations.Add("Dead Evaders(O_d)", tmpPointList);

    //        pgui.markLocations(markedLocations);

    //        InputRequest req = new InputRequest();
    //        IEnumerable<Evader> relevantEvaders;
    //        if (currentRound == 0)
    //            relevantEvaders = gm.A_E;
    //        else
    //            relevantEvaders = s.ActiveEvaders;

    //        foreach (Evader e in relevantEvaders)
    //        {
    //            if (currentEvadersLocations[e].locationType != Location.Type.Node ||
    //                (currentEvadersLocations[e].nodeLocation.manDist(RoutingAreaCenter) > gm.r_e))
    //                req.addMovementOption(e, currentEvadersLocations[e], getPossibleNextLocations(currentEvadersLocations[e]));

    //            prevChoiceKey = req.addComboChoice(e, "communicate", new string[] { "yes", "no" }, "no");
    //        }

    //        pgui.setInputRequest(req);
    //        nextEvadersToCommunicate.Clear();
    //    }

    //    public override Dictionary<Evader, Point> getNextStep()
    //    {
    //        Dictionary<Evader, Point> res = new Dictionary<Evader, Point>();
    //        IEnumerable<Evader> relevantEvaders;

    //        if (prevS.ActiveEvaders.Count == 0)
    //            relevantEvaders = gm.A_E; //first round
    //        else
    //            relevantEvaders = prevS.ActiveEvaders;

    //        foreach (Evader e in relevantEvaders)
    //        {
    //            if (currentEvadersLocations[e].nodeLocation.manDist(RoutingAreaCenter) > gm.r_e)
    //            {
    //                res[e] = pgui.getMovement(e).First().nodeLocation;
    //                currentEvadersLocations[e] = pgui.getMovement(e).First();
    //            }
    //            else
    //            {
    //                // evader is on Routing circumference - it is now stuck!
    //                res[e] = currentEvadersLocations[e].nodeLocation;
    //            }
    //        }
    //        return res;
    //    }

    //    private List<Location> getPossibleNextLocations(Location currentAgentLocation)
    //    {
    //        List<Location> res = new List<Location>();
    //        res.Add(currentAgentLocation);

            
    //            res.AddRange(GameLogic.Utils.pointsToLocations(g.getNodesWithinDistance(RoutingAreaCenter, gm.r_e + 5)));
            

    //        return res;
    //    }

    //    public override Dictionary<Evader, Point> communicate()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
