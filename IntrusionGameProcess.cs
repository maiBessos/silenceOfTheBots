using GoE.Policies;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.Utils.Algorithms;

namespace GoE.GameLogic
{
    public class IntrusionGameProcess : AGameProcess
    {
        private GameGraph<Point> gr;
        private GameLogic.Utils.Grid4Square sensitiveAreaSquare = null; // used only if gm.IsSquareArea=true
        private IntrusionGameParams gm; // gamma -- all game constants
        private AIntrusionPursuersPolicy Pi_p;
        private AIntrusionEvadersPolicy Pi_e;
        private IntrusionGameState state;
        private int t; // current round
        private delegate void startFunc();

        private bool pursuerTurn = true;
        private Point intrusionAreaCenter;
        private List<Point> O_c = new List<Point>(); // evaders observed by pursuers (due evader communicating)
        private List<Utils.CapturedObservation> O_d1 = new List<Utils.CapturedObservation>();
        private List<Utils.CapturedObservation> O_d2 = new List<Utils.CapturedObservation>();
        private List<Utils.PursuerPathObservation> O_p = new List<Utils.PursuerPathObservation>(); // pursuers observed by evaders (due proximity)
        //private HashSet<Point> O_e = new HashSet<Point>(); // evaders observed by pursuers (due proximity)
        private int remainingEvaders;


        public double GameResultReward
        {
            get
            {
                if(IsFinished)
                    return (remainingEvaders == 0)?(0):(1);
                return 0;
            }
        }

        
        //public IntrusionGameProcess(IntrusionGameParams  param, GameGraph<Point> GameGraph)
        public void initParams(IGameParams param, AGameGraph GameGraph)
        {
            this.LatestIntruderLocations = new Dictionary<Evader, Point>();
            this.LatestPursuerLocations = new Dictionary<Pursuer, List<Point>>();
            this.gr = (GameGraph<Point>)GameGraph;
            this.gm = (IntrusionGameParams)param;
            this.intrusionAreaCenter = gr.getNodesByType(NodeType.Target).First();
            if (gm.IsAreaSquare)
            {
                sensitiveAreaSquare = gm.SensitiveAreaSquare(intrusionAreaCenter);
            }
        }

        /// <summary>
        /// expects initialized policies
        /// </summary>
        public void init(APursuersPolicy ipursuerJointPolicy,
                         IEvadersPolicy ievadersJointPolicy)
        {
            
            this.Pi_p = (AIntrusionPursuersPolicy)ipursuerJointPolicy;
            this.Pi_e = (AIntrusionEvadersPolicy)ievadersJointPolicy;
            state = new IntrusionGameState();
            remainingEvaders = gm.A_E.Count;
            pursuerTurn = true;

            initGameState();

            Pi_p.setGameState(t, O_c, O_d1.Union(O_d2));

            IsFinished = false;
            
        }
        
        List<Point> currentPursuersLocations = new List<Point>(); // helps optimizing checlDestroyedAgents

        public Dictionary<Evader, Point> LatestIntruderLocations { get; set; }

        /// <summary>
        /// after GameProcess() is constructed, we assume invokeNextPolicy()
        /// is called continously until it returns false.
        /// 
        /// The first invokeNextPolicy() will invoke the given IPursuersPolicy's getNextStep(), 
        /// The next call will invoke IEvadersPolicy's getNextStep(), then it alternated again.
        /// 
        /// between each calls, it is expected that any IPolicyGUI requests will be handled
        /// 
        /// Follows as closly as possible to GoE "Game Process" procedure
        /// </summary>
        /// <returns>
        /// true as long as there are living evaders
        /// (if treturned false, then finishGame() was called)
        /// </returns>
        public bool invokeNextPolicy()
        {
            if(pursuerTurn)
            {
                pursuerTurn = false;
                LatestPursuerLocations = Pi_p.getNextStep();

                //// TODO remove below
                //if (state.ActiveEvaders.Count > 0 && state.MostUpdatedEvadersMemoryRound > 1 &&
                //    state.M[state.MostUpdatedEvadersMemoryRound][state.ActiveEvaders.First()] == null)
                //{
                //    while (true) ;
                //}

                currentPursuersLocations.Clear();
                foreach (var pursuerLocation in LatestPursuerLocations)
                {
                    //state.L[t][pursuerLocation.Key] = new Location(pursuerLocation.Value.Last());
                    currentPursuersLocations.Add(pursuerLocation.Value.Last());
                }

                O_p = new List<Utils.PursuerPathObservation>();
                //O_e = new HashSet<Point>();

                if (t > 0)
                {
                    checkDestroyedAgents(ref O_d1, t - 1, LatestPursuerLocations);

                    #region populate O_P
                    // and O_e ?

                    //var pursuerLocations = state.L[t];
                    //var eveLocations = state.L[t - 1];
                    // TODO: the pointset structure can be used to speed this up significantly
                    foreach (Pursuer p in gm.A_P)
                    {
                        List<Point> pLocList = LatestPursuerLocations[p];
                        foreach (var eve in state.ActiveEvaders)
                        {
                            Point eLoc = LatestIntruderLocations[eve];
                            var path = new Utils.PursuerPathObservation(t, eLoc, eve, gm.r_es, pLocList);
                            if(path.observedPursuerPath.Count >= 1)
                                O_p.Add(path);
                        }
                    }
                }
                #endregion
                O_c = new List<Point>();

                //// TODO remove below
                //if (state.ActiveEvaders.Count > 0 && state.MostUpdatedEvadersMemoryRound > 1 &&
                //    state.M[state.MostUpdatedEvadersMemoryRound][state.ActiveEvaders.First()] == null)
                //{
                //    while (true) ;
                //}

                state.MostUpdatedPursuersRound = t;
                Pi_e.setGameState(t, O_d1.Union(O_d2), O_p, state);

                //// TODO remove below
                //if (state.ActiveEvaders.Count > 0 && state.MostUpdatedEvadersMemoryRound > 1 &&
                //    state.M[state.MostUpdatedEvadersMemoryRound][state.ActiveEvaders.First()] == null)
                //{
                //    while (true) ;
                //}
           } 
            else
            {
                pursuerTurn = true;

                //// TODO remove below
                //if (state.ActiveEvaders.Count > 0 && state.MostUpdatedEvadersMemoryRound > 1 &&
                //    state.M[state.MostUpdatedEvadersMemoryRound][state.ActiveEvaders.First()] == null)
                //{
                //    while (true) ;
                //}


                var communicatingEvaders = Pi_e.communicate();
                Dictionary<Evader, Point> policyOut = Pi_e.getNextStep();
                O_c = communicatingEvaders.Select(x => policyOut[x]).ToList();
                if(t == 0)
                { 
                    state.ActiveEvaders.Clear();
                    foreach (var evaderOut in policyOut)
                        state.ActiveEvaders.Add(evaderOut.Key);
                }

                LatestIntruderLocations = policyOut;
                //foreach (var evaderOut in policyOut)
                  //  state.L[t][evaderOut.Key] = new Location(evaderOut.Value);
                
                checkDestroyedAgents(ref O_d2, t, currentPursuersLocations);
                
                foreach(Evader e in state.ActiveEvaders)
                {
                    if (remainingRoundsToIntrusion.ContainsKey(e))
                    {
                        if (--remainingRoundsToIntrusion[e] <= 0)
                        {
                            finishGame();
                            return false;
                        }
                    }
                    else
                    {
                        if(gm.IsAreaSquare)
                        {
                            if(sensitiveAreaSquare.isOnSquare(LatestIntruderLocations[e]))
                                remainingRoundsToIntrusion[e] = gm.t_i;
                        }
                        else if(LatestIntruderLocations[e].manDist(intrusionAreaCenter)==gm.r_e)
                            remainingRoundsToIntrusion[e] = gm.t_i;
                    }
                }
                state.MostUpdatedEvadersLocationRound = t;
                ++t;
                //state.L.Add(new Dictionary<IAgent, Location>());

                //// TODO remove below
                //if (state.ActiveEvaders.Count > 0 && state.MostUpdatedEvadersMemoryRound > 1 &&
                //    state.M[state.MostUpdatedEvadersMemoryRound][state.ActiveEvaders.First()] == null)
                //{
                //    while (true) ;
                //}

                Pi_p.setGameState(t, O_c, O_d1.Union(O_d2));

                //// TODO remove below
                //if (state.ActiveEvaders.Count > 0 && state.MostUpdatedEvadersMemoryRound > 1 &&
                //    state.M[state.MostUpdatedEvadersMemoryRound][state.ActiveEvaders.First()] == null)
                //{
                //    while (true) ;
                //}
            }

            if(remainingEvaders <= 0)
            {
                finishGame();
                return false;
            }
            return true;
            
        }

        /// <summary>
        /// set to true when finishGame() is called
        /// </summary>
        public bool IsFinished { get; protected set; }

        
        /// <summary>
        /// automatically called if on getNextStep() all evaders are captured.
        /// may be force called if the game ends abruptly (e.g. for performance estimation)
        /// </summary>
        public void finishGame()
        {
            // due to performance reasons, we add reward on the fly instead of game finish
            IsFinished = true;
            
            
        }

        public int CapturedEvaders 
        {
            get
            {
                return Params.A_E.Count - remainingEvaders;
            }
        }

        public Dictionary<Pursuer, List<Point>> LatestPursuerLocations { get; protected set; }
        public IntrusionGameState State { get { return state;  } }
        public IntrusionGameParams Params { get { return gm; } }
        public int currentRound { get { return t; } }

        public IDictionary<string, string> ResultValues
        {
            get
            {
                return null;
            }
        }

        private void initGameState()
        {
            //state.L.Clear();
            
            //state.L.Add(new Dictionary<IAgent, Location>());

            //foreach (Evader e in gm.A_E)   
            //    state.L[0][e] = new Location(Location.Type.Unset);
             
            //foreach (Pursuer p in gm.A_P)
            //    state.L[0][p] = new Location(Location.Type.Unset);

            t = 0;
            

            state.MostUpdatedPursuersRound = -1;
            state.MostUpdatedEvadersLocationRound = -1;
        }
        private Dictionary<Evader, int> remainingRoundsToIntrusion = new Dictionary<Evader,int>();

        private void checkDestroyedAgents(ref List<Utils.CapturedObservation> O_d, int et, Dictionary<Pursuer, List<Point>> latestPursuersPaths)
        {
            List<Point> visitedPursuerPoints = new List<Point>();
            foreach (var pl in latestPursuersPaths)
                visitedPursuerPoints.AddRange(pl.Value);
            checkDestroyedAgents(ref O_d, et, visitedPursuerPoints);
        }
        private void checkDestroyedAgents(ref List<Utils.CapturedObservation> O_d, int et, List<Point> visitedPursuerPoints)
        {
            O_d = new List<Utils.CapturedObservation>();

            PointSet puruserLocations =
                new PointSet(visitedPursuerPoints);

            //var locations = state.L[et];

            //foreach (Evader e in gm.A_E)
            foreach (var e in LatestIntruderLocations)
            {   
                //if(!locations.ContainsKey(e))
                //    continue;

                //var eveLoc = locations[e];
                //if (eveLoc.locationType != Location.Type.Node)
                //    continue;
                //if (puruserLocations.Contains(eveLoc.nodeLocation))
                if (puruserLocations.Contains(e.Value))
                {
                    //state.ActiveEvaders.Remove(e);
                    state.ActiveEvaders.Remove(e.Key);
                    //O_d.Add(new Utils.CapturedObservation() { where = eveLoc.nodeLocation, who = e });
                    O_d.Add(new Utils.CapturedObservation() { where = e.Value, who = e.Key });
                    //locations[e] = new Location(Location.Type.Captured);
                }

                //if (locations[e].locationType == Location.Type.Captured)
                //    locations[e] = new Location(Location.Type.Captured);
            }
            foreach(var de in O_d)
                LatestIntruderLocations.Remove(de.who);
            
            remainingEvaders -= O_d.Count;
        }

        public string getGraphTypename()
        {
            return GoE.Utils.ReflectionUtils.GetObjStaticTypeName(gr);
        }
        public IGameParams constructGameParams()
        {
            return new IntrusionGameParams();
        }

        public IGameProcessGUI constructGUI()
        {
            return new frmIntrusionGameProcessView();
        }

        public Type getRouterPolicyBaseType()
        {
            throw new NotImplementedException();
        }

        public Type getPursuerPolicyBaseType()
        {
            throw new NotImplementedException();
        }
    }
}
