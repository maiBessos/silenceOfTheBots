using GoE.Policies;
using GoE.Utils.Algorithms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GoE.Utils.Algorithms;
using GoE.UI;

namespace GoE.GameLogic
{
    public interface IGameProcessGUI : IPolicyGUIInputProvider
    {
        void init(AGameGraph grGraph, AGameProcess p);
        #region abstract IPolicyGUIInputProvider impl.
        void addCurrentRoundLog(List<string> logLines);
        void addLogValue(string key, string value);
        void debugStopSkippingRound();
        void flushLog();
        object getChoice(IAgent chooser, int choiceKey);
        List<Location> getMovement(IAgent mover);
        bool hasBoardGUI();
        void markLocations(Dictionary<string, List<PointF>> locations);
        void setInputRequest(InputRequest req);
        #endregion
    }
    public interface AGameProcess //: GoE.Utils.ReflectionUtils.DerivedTypesProvider<AGameProcess>
    {
        /// <summary>
        /// returns an uninitialized gui 
        /// object is assumed to also inherit class 'Form'
        /// (A class that inherits from both Form and IGameProcessGUI  isn't defined since it makes problems with csharp designer)
        /// </summary>
        /// <returns></returns>
        IGameProcessGUI constructGUI();

        Type getRouterPolicyBaseType();
        Type getPursuerPolicyBaseType();

        string getGraphTypename();

        /// <summary>
        /// called immediately after construction
        /// </summary>
        /// <param name="param"></param>
        /// <param name="GameGraph"></param>
        void initParams(IGameParams param, AGameGraph GameGraph);

        bool IsFinished { get; }

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
        bool invokeNextPolicy();

        void init(APursuersPolicy pursuerJointPolicy,
                         IEvadersPolicy evadersJointPolicy);

        /// <summary>
        /// returns an uninitialized instance of the coresponding param type class
        /// </summary>
        /// <returns></returns>
        IGameParams constructGameParams();

        /// <summary>
        /// automatically called if on getNextStep() all evaders are captured.
        /// may be force called if the game ends abruptly (e.g. for performance estimation)
        /// </summary>
        void finishGame();

        int currentRound { get; }

        double GameResultReward { get; }

        int CapturedEvaders { get; }


        /// <summary>
        /// Used for local search algorithms (which use game value estimation).
        /// e.g. for values such as heuristic estimation of final reward (if game was ended abruptly)
        /// No need to include GameResultReward and CpaturedEvaders.
        /// </summary>
        IDictionary<string, string> ResultValues { get; }
    }
    public class GoEGameProcess : AGameProcess
    {
        private Random myRand;
        private GameGraph<Point> gr;
        private GoEGameParams gm; // gamma -- all game constants
        private AGoEPursuersPolicy Pi_p;
        private AGoEEvadersPolicy Pi_e;
        private GameState state;
        private int t; // current round
        private delegate void startFunc();

        private bool pursuerTurn = true;

        private List<Point> O_c = new List<Point>();
        private List<Point> O_d1 = new List<Point>();
        private List<Point> O_d2 = new List<Point>();
        private HashSet<Point> O_p = new HashSet<Point>();
        private int remainingEvaders;

        //public GameProcess(GameParams param, GameGraph<Point> GameGraph)
        public void initParams(IGameParams param, AGameGraph GameGraph)
        {
            this.gr = (GameGraph<Point>)GameGraph;
            this.gm = (GoEGameParams)param;
        }

        
        [ThreadStatic]
        private static GoE.Utils.Algorithms.PointSet transmitPoints; // tells the point from which evaders may transmit
        [ThreadStatic]
        private static int currentlyAllocatedTransmitPointsRE;
        GoE.GameLogic.Utils.DataUnitVec dataInSink;

        public IGameProcessGUI constructGUI()
        {
            return new frmGameProcessView();
        }
        /// <summary>
        /// expects initialized policies
        /// </summary>
        public void init(APursuersPolicy ipursuerJointPolicy, 
                         IEvadersPolicy ievadersJointPolicy)
        {
            this.Pi_p = (AGoEPursuersPolicy)ipursuerJointPolicy;
            this.Pi_e = (AGoEEvadersPolicy)ievadersJointPolicy;
            state = new GameState();
            remainingEvaders = gm.A_E.Count;
            pursuerTurn = true;
            myRand = new EvolutionaryStrategy.ThreadSafeRandom().rand;

            initGameState();

            Pi_p.setGameState(t, O_c, O_d1.Union(O_d2));

            IsFinished = false;
            sinks = gr.getNodesByType(NodeType.Sink);
            sinksSearch = new GoE.Utils.Algorithms.PointSet(sinks);


            AccumulatedEvadersReward = 0;
            if (transmitPoints == null || currentlyAllocatedTransmitPointsRE != gm.r_e)
            {
                
                // we don't count reward as written in the paper(in the game end), to improve performance
                //HashSet<Point> transmitPoints = new HashSet<Point>();
                //GoE.Utils.AlgorithmUtils.PointSet transmitPoints;
                List<Point> dummytransmitPoints = new List<Point>(sinks.Count * (2 * gm.r_e * (gm.r_e + 1) + 1));
                // tells from where an evader may transmit so the transmission reaches a sink
                foreach (var s in sinks)
                {
                    var tmpTransmitPoints = gr.getNodesWithinDistance(s, gm.r_e);
                    foreach (var p in tmpTransmitPoints)
                    {
                        //transmitPoints.Add(p);
                        dummytransmitPoints.Add(p);
                    }
                }

                currentlyAllocatedTransmitPointsRE = gm.r_e;
                transmitPoints = new PointSet(dummytransmitPoints);
                transmitPoints.removeDupliacates();
            }

            dataInSink = new GoE.GameLogic.Utils.DataUnitVec();
            dataInSink.Add(DataUnit.NIL);
            dataInSink.Add(DataUnit.NOISE); // we add NIL and noise so we don't count them as new units
        }

        void updateEvadersReward()
        {
            foreach (Evader ei in state.ActiveEvaders)
            {
                if (state.B_O[state.MostUpdatedEvadersLocationRound].ContainsKey(ei))
                {
                    
                    var currentTansmittedData = state.B_O[state.MostUpdatedEvadersLocationRound][ei];
                    // flush moves all the data you have in memory to sink (including data ader accumulated AS it entered the sink)
                    if (currentTansmittedData == DataUnit.Flush)
                    {
                        var eveLoc = state.L[state.MostUpdatedEvadersLocationRound][ei];
                        if (sinksSearch.Contains(eveLoc.nodeLocation))
                        {
                            var flushedData = state.M[state.MostUpdatedEvadersMemoryRound][ei].roundRanges;
                            foreach (var u in flushedData)
                                for (int ur = u.minRound; ur <= u.maxRound; ++ur)
                                    if (dataInSink.Add(new DataUnit() { round = ur }))
                                        AccumulatedEvadersReward += gm.R.getReward(state.MostUpdatedEvadersLocationRound - ur);
                        }
                        continue;
                    }
                }

                if (state.B_O[state.MostUpdatedEvadersLocationRound].ContainsKey(ei))
                {
                    var prevTransmittedData = state.B_O[state.MostUpdatedEvadersLocationRound][ei];
                    if (prevTransmittedData == DataUnit.NIL)
                        continue;
                    var prevEveLoc = state.L[state.MostUpdatedEvadersLocationRound][ei];
                    if (transmitPoints.Contains(prevEveLoc.nodeLocation) && !dataInSink.Contains(prevTransmittedData))
                    {
                        AccumulatedEvadersReward += gm.R.getReward(state.MostUpdatedEvadersLocationRound - prevTransmittedData.round - 1);
                        dataInSink.Add(prevTransmittedData);
                    }
                }
            }
        }

        List<Point> currentPursuersLocations = new List<Point>(); // helps optimizing checlDestroyedAgents

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
                Dictionary<Pursuer, Location> nextLocations = Pi_p.getNextStep();

                //// TODO remove below
                //if (state.ActiveEvaders.Count > 0 && state.MostUpdatedEvadersMemoryRound > 1 &&
                //    state.M[state.MostUpdatedEvadersMemoryRound][state.ActiveEvaders.First()] == null)
                //{
                //    while (true) ;
                //}

                currentPursuersLocations.Clear();
                foreach (var pursuerLocation in nextLocations)
                {
                    state.L[t][pursuerLocation.Key] = pursuerLocation.Value;
                    currentPursuersLocations.Add(pursuerLocation.Value.nodeLocation);
                }

                checkDestroyedAgents(ref O_d1, t - 1, t, 0); // we use EvaderCircumferenceEntranceKillProb = 0 since this test is used only when EVADERS move, not when pursuers move


                updateEvadersMemory();
                state.MostUpdatedEvadersMemoryRound = t;
                updateEvadersReward();

                //// TODO remove below
                //if (state.ActiveEvaders.Count > 0 && state.MostUpdatedEvadersMemoryRound > 1 &&
                //    state.M[state.MostUpdatedEvadersMemoryRound][state.ActiveEvaders.First()] == null)
                //{
                //    while (true) ;
                //}

                #region populate O_P
                
                O_p = new HashSet<Point>();

                // the commented code is VERY slow (profiler's estimation)
                //List<Point> sensingPoints = new List<Point>();
                //sensingPoints.AddRange(gr.getNodesByType(NodeType.Sink));
                //foreach (Evader e in gm.A_E)
                //    if (state.L[t-1][e].locationType == Location.Type.Node)
                //        sensingPoints.Add(state.L[t-1][e].nodeLocation);

                //foreach (Pursuer p in gm.A_P)
                //{
                //    if (state.L[t][p].locationType != Location.Type.Node)
                //        continue;
                //    foreach (Point s in sensingPoints)
                //        if (gr.getMinDistance(s, state.L[t][p].nodeLocation) <= gm.r_s)
                //            O_p.Add(state.L[t][p].nodeLocation);
                //}

                var pursuerLocations = state.L[t];
                var eveLocations = state.L[t-1];
                foreach (Pursuer p in gm.A_P)
                {
                    Point pLoc = pursuerLocations[p].nodeLocation;
                    foreach(var eve in state.ActiveEvaders)
                    {
                        if (eveLocations[eve].nodeLocation.manDist(pursuerLocations[p].nodeLocation) <= gm.r_s)
                            O_p.Add(pLoc);
                    }

                    // note: this loop is very slow! :
                    if(Params.canSinksSensePursuers)
                        foreach(var si in sinks)
                        {
                            if (si.manDist(pursuerLocations[p].nodeLocation) <= gm.r_s)
                                O_p.Add(pLoc);
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
                Dictionary<Evader, Tuple<DataUnit, Location, Location>> policyOut = Pi_e.getNextStep();
                state.ActiveEvaders.Clear();
                foreach (var evaderOut in policyOut)
                {
                    state.B_O[t][evaderOut.Key] = evaderOut.Value.Item1;
                    state.L[t][evaderOut.Key] = evaderOut.Value.Item2;
                    state.B_I[t][evaderOut.Key] = evaderOut.Value.Item3;

                    if (evaderOut.Value.Item2.locationType == Location.Type.Node)
                        state.ActiveEvaders.Add(evaderOut.Key);

                    if (evaderOut.Value.Item1 != DataUnit.NIL)
                    {
                        //O_c.Add(evaderOut.Value.Item2.nodeLocation); //this lets the pursuers know the DESTINATION of the evader

                        if(myRand.NextDouble() <= Params.p_d)
                            O_c.Add(state.L[t-1][evaderOut.Key].nodeLocation); //this on the other hand lets the pursuers know the start location of the evader
                    }
                }

                checkDestroyedAgents(ref O_d2, t, t, Params.EvaderCircumferenceEntranceKillProb);
                state.MostUpdatedEvadersLocationRound = t;
                ++t;

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

        GoE.Utils.Algorithms.PointSet sinksSearch;  
        List<Point> sinks;

        public IGameParams constructGameParams()
        {
            return new GoEGameParams();
        }
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

        /// <summary>
        /// gets set to the total reward of the evaders, after gameFinish() is called
        /// </summary>
        public double AccumulatedEvadersReward { get; protected set; }


        public double GameResultReward 
        {
            get
            {
                return AccumulatedEvadersReward / CapturedEvaders;
            }
        }

        public GameState State { get { return state;  } }
        public GoEGameParams Params { get { return gm; } }
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
            foreach (Evader e in gm.A_E)
            {
                state.L[-1][e] = new Location(Location.Type.Unset);
                state.L[0][e] = new Location(Location.Type.Unset);
                state.B_I[-1][e] = new Location(Location.Type.Undefined);
                state.B_O[-1][e] = DataUnit.NIL;
                state.M[0][e] = new GoE.GameLogic.Utils.DataUnitVec();
                state.M[0][e].Add(DataUnit.NOISE);

                // these 2 assignemnets are not in the formal game definition, but helps sparing an if() for some policy implementations
                state.M[-1][e] = new GoE.GameLogic.Utils.DataUnitVec();
                state.M[-1][e].Add(DataUnit.NOISE); 

            }
            t = 0;
            
            foreach (Pursuer p in gm.A_P)
            {
                state.L[-1][p] = new Location(Location.Type.Unset);
                state.L[0][p] = new Location(Location.Type.Unset);
            }

            state.MostUpdatedPursuersRound = -1;
            state.MostUpdatedEvadersLocationRound = -1;
            state.MostUpdatedEvadersMemoryRound = -1;
        }
        private void updateEvadersMemory()
        {
            
            foreach (Evader ei in gm.A_E)
            {

                //if (!state.L[state.MostUpdatedEvadersLocationRound].Keys.Contains(ei) || state.L[state.MostUpdatedEvadersLocationRound][ei].locationType != Location.Type.Node)
                //if (state.L[state.MostUpdatedEvadersLocationRound][ei].locationType == Location.Type.Captured)
                //    continue;

                state.M[t][ei] = state.M[state.MostUpdatedEvadersMemoryRound][ei];
                state.M[state.MostUpdatedEvadersMemoryRound][ei] = null;
                //for (int i = 0; i < t - 1; ++i) // Fixme: even though deleting previous memory states doesn't hurt any policy, it MIGHT!
                    //state.M[i] = null;

                if (!state.L[state.MostUpdatedEvadersLocationRound].Keys.Contains(ei) || state.L[state.MostUpdatedEvadersLocationRound][ei].locationType != Location.Type.Node)
                    continue;

                // if the target was in radius when evader started listening to it, add it:
                if (state.B_I[state.MostUpdatedEvadersLocationRound][ei].locationType == Location.Type.Node &&
                   gr.getMinDistance(state.L[state.MostUpdatedEvadersLocationRound-1][ei].nodeLocation,
                                     state.B_I[state.MostUpdatedEvadersLocationRound][ei].nodeLocation) <= gm.r_e)
                {
                    state.M[t][ei].Add(new DataUnit() { sourceTarget = state.B_I[state.MostUpdatedEvadersLocationRound][ei], round = state.MostUpdatedEvadersLocationRound });
                }


                if(Params.canEvadersReceiveMultipleBroadcasts)
                {
                    var targetMem = state.M[t][ei];
                    var currentB_O = state.B_O[state.MostUpdatedEvadersLocationRound];
                    DataUnit newDataUnit = DataUnit.NIL;
                    foreach (Evader ej in currentB_O.Keys)
                    {
                        if (ei == ej || targetMem.Contains(currentB_O[ej]))
                            continue;


                        var prevLocations = state.L[state.MostUpdatedEvadersLocationRound - 1];
                        if (!prevLocations.ContainsKey(ei) || !prevLocations.ContainsKey(ej))
                            continue;

                        // if transmission was within distance when it started:
                        if (gm.r_e >= gr.getMinDistance(prevLocations[ei].nodeLocation,
                                                        prevLocations[ej].nodeLocation))
                        {
                            
                            if (state.L[state.MostUpdatedEvadersLocationRound].ContainsKey(ej) && 
                                state.L[state.MostUpdatedEvadersLocationRound][ej].locationType != Location.Type.Node)
                            {
                                newDataUnit = DataUnit.NIL; // some evader that tried sending data was destroyed mid transmission - this is considered noise anyway
                                break;
                            }
                            
                            targetMem.Add(currentB_O[ej]);
                        }
                    }
                    
                }
                else
                {
                    // O lists the evaders that transmitted data, and ei was able to receive it:
                    List<Evader> O = new List<Evader>();
                    foreach (Evader ej in state.B_O[state.MostUpdatedEvadersLocationRound].Keys)
                    {
                        if (ei == ej)
                            continue;

                        if (state.B_O[state.MostUpdatedEvadersLocationRound][ej] != DataUnit.NIL &&
                            state.B_O[state.MostUpdatedEvadersLocationRound][ej] != DataUnit.NOISE &&
                            gm.r_e >= gr.getMinDistance(state.L[state.MostUpdatedEvadersLocationRound-1][ei].nodeLocation, 
                                                        state.L[state.MostUpdatedEvadersLocationRound-1][ej].nodeLocation))
                        {
                            O.Add(ej);
                        }
                    }

                    // if more than 1 packets were received, it is considered noise due to collision
                    if (O.Count == 1 &&
                        !(state.L[state.MostUpdatedEvadersLocationRound].ContainsKey(O.First()) && state.L[state.MostUpdatedEvadersLocationRound][O.First()].locationType != Location.Type.Node)) // also make sure the evader is still alive
                    {
                        state.M[t][ei].Add(state.B_O[state.MostUpdatedEvadersLocationRound][O.First()]);
                    }
                }
            }
        }

        
        private void checkDestroyedAgents(ref List<Point> O_d, int et, int pt, float EvaderCircumferenceEntranceKillProb)
        {
            O_d = new List<Point>();

            PointSet puruserLocations = new PointSet(currentPursuersLocations);

            //HashSet<Point> puruserLocations = new HashSet<Point>();
            //foreach (Pursuer p in gm.A_P)
            //{
            //    if (state.L[pt][p].locationType == Location.Type.Node)
            //        puruserLocations.Add(state.L[pt][p].nodeLocation);
            //}

            // since sinks are safe points, we exclude them 
            if (gm.areSinksSafe)
            {
                //if (sinks.Count > puruserLocations.Count) // profiling says the operation is heavy - we ry minimizing the Contains() queries
                //{
                //    puruserLocations.RemoveWhere((x) => sinks.Contains(x));
                //}
                //else
                //{
                    foreach (Point p in sinks)
                        puruserLocations.Remove(p);
                //}
            }

            var locations = state.L[et];
            foreach (Evader e in gm.A_E)
            {   
                if(!locations.ContainsKey(e))
                    continue;

                var eveLoc = locations[e];
                
                if (eveLoc.locationType != Location.Type.Node)
                    continue;

                if (EvaderCircumferenceEntranceKillProb > 0.001)
                {
                    // evaders that just entered the circumference face a chance to get killed immediately
                    if (eveLoc.nodeLocation.manDist(gr.getNodesByType(NodeType.Target).First()) == Params.r_e &&
                        state.L[et - 1][e] != eveLoc)
                    {
                        if (myRand.NextDouble() <= EvaderCircumferenceEntranceKillProb)
                        {
                            state.ActiveEvaders.Remove(e);
                            O_d.Add(eveLoc.nodeLocation);
                            locations[e] = new Location(Location.Type.Captured);
                        }
                    }
                }
                else if (puruserLocations.Contains(eveLoc.nodeLocation))
                {
                    state.ActiveEvaders.Remove(e);
                    O_d.Add(eveLoc.nodeLocation);
                    locations[e] = new Location(Location.Type.Captured);
                }

                // FIXME: is this a bug? the if() below shouldn't we check here locations[e-1]?
                //if (locations[e].locationType == Location.Type.Captured)
                //    locations[e] = new Location(Location.Type.Captured);
            }

            
            remainingEvaders -= O_d.Count;
        }

        public string getGraphTypename()
        {
            return GoE.Utils.ReflectionUtils.GetObjStaticTypeName(gr);
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
