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
using GoE.GameLogic.EvolutionaryStrategy;

namespace GoE.GameLogic
{
    public class AgentGrid<T> where T : IAgent
    {
        public AgentGrid(Rectangle area)
        {
            agents = new List<List<T>>(area.Width);
            isOccupied = new List<List<bool>>(area.Width);
            for (int x = 0; x < area.Width; ++x)
            {
                isOccupied.Add(AlgorithmUtils.getRepeatingValueList(false, area.Height));
                agents.Add(AlgorithmUtils.getRepeatingValueList<T>(null, area.Height));           
            }

        }

        public T this[int X, int Y]
        {
            get
            {
                return agents[X][Y];
            }
            set
            {
                agents[X][Y] = value;
            }
        }
        public T this[Point location]
        {
            get
            {
                return agents[location.X][location.Y];
            }
            set
            {
                agents[location.X][location.Y] = value;
            }
        }
        public List<bool> getColumn(int x)
        {
            return isOccupied[x];
        }

        private List<List<bool>> isOccupied;
        private List<List<T>> agents;
    }

    /// <summary>
    /// game between two distant 'fronts' that try to communicate. routers replenish
    /// </summary>
    public class FrontsGridRoutingGameProcess : AGameProcess
    {
        private GridGameGraph graph;
        
        private FrontsGridRoutingGameParams gm; // all game constants
        private AFrontsGridRoutingPursuersPolicy Pi_p;
        private AFrontsGridRoutingEvadersPolicy Pi_e;
        
        private bool pursuerTurn;

        private AgentGrid<Evader> EvaderLocations
        {
            get; set;
        }
        private double transmissionDetectionProb;
        private ThreadSafeRandom rand ;
        public int GameStep { get; set; }
        public List<Point> LatestPursuerLocations { get; protected set; }
        //public List<Point> LatestEvaderLocations { get; protected set; }
        public HashSet<Point> AllEvaderLocations { get; protected set; }
        public CommunicationGraph O_c { get; protected set; } // evaders observed by pursuers (due evader communicating)
        public List<Point> AllTransmittingEvaders { get; protected set; }
        public List<Point> DetectedEvaders { get; protected set; }
        public HashSet<Utils.CapturedObservation> O_d { get; protected set; }

        const int STEP_GROUP_SIZE = 50; // FIXME: this is a dirty fix for "RewardPortion". we group every GROUP_SIZE steps, and only keep the reward

        // for each GROUP_SIZE steps, this keeps the total reward and "CapturedEvaders"
        private class RewardGroup
        {
            public RewardGroup(int Captured)
            {
                this.capturedEbots = Captured;
            }
            public double reward = 0;
            public int capturedEbots = 0;
        }
        List<RewardGroup> rewardGroups; 


        private int nextEvaderIdxToAllocate; // if evaders have enough credit, allocate evader with next index. 
        private float evadersCredit; // increases according to evader renewal rate. when >1 , new evaders are added to the game

        public IGameProcessGUI constructGUI()
        {
            return new frmFrontsGridRoutingGameProcessView();
        }

        public double transferredData;
        public double GameResultReward
        {
            get
            {
                return transferredData / CapturedEvaders;
            }
        }

        /// <summary>
        /// called right after ctor
        /// </summary>
        /// <param name="param"></param>
        /// <param name="GameGraph"></param>
        public void initParams(IGameParams param, AGameGraph GameGraph)
        {
            this.graph = (GridGameGraph)GameGraph;
            this.gm = (FrontsGridRoutingGameParams)param;

            double detection = (1 + (Math.Pow(gm.detectionProbRestraint * (gm.r_e - 1), 3)));
            this.transmissionDetectionProb =
                1.0 - 1.0 / detection;
                
        }

        /// <summary>
        /// expects initialized policies
        /// </summary>
        public void init(APursuersPolicy ipursuerJointPolicy,
                         IEvadersPolicy ievadersJointPolicy)
        {
            GameStep = 0;
            rewardGroups = new List<RewardGroup>();
            O_d = new HashSet<Utils.CapturedObservation>();
            this.O_c = new CommunicationGraph();
            this.Pi_p = (AFrontsGridRoutingPursuersPolicy)ipursuerJointPolicy;
            this.Pi_e = (AFrontsGridRoutingEvadersPolicy)ievadersJointPolicy;
            this.rand = new ThreadSafeRandom();
            




            EvaderLocations = new AgentGrid<Evader>(
                new Rectangle(0, 0, (int)graph.WidthCellCount, (int)graph.HeightCellCount));

            //LatestEvaderLocations = new List<Point>();
            LatestPursuerLocations = new List<Point>();
            AllEvaderLocations = new HashSet<Point>();

            transferredData = 0;

            evadersCredit = 0;
            nextEvaderIdxToAllocate = 0;
            CapturedEvaders = 0;

            pursuerTurn = true;
            IsFinished = false;
            MostUpdatedPursuersRound = 0;
            MostUpdatedEvadersRound = 0;

            Pi_p.setGameState(MostUpdatedPursuersRound, new List<Point>(), O_d);



            if (Params.A_E.Count < graph.Nodes.Count + 5 * graph.WidthCellCount)
                throw new Exception("Not enough E-bots"); // since the game stops after remaining evader count is less than graph.Nodes.Count, we must make sure the evaluation is valid
        }
        
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
                LatestPursuerLocations = Pi_p.getNextStep();
                ++MostUpdatedPursuersRound;
                
                var newDead = checkDestroyedAgents(LatestPursuerLocations);
                O_d = new HashSet<Utils.CapturedObservation>();
                foreach (var p in newDead)
                    O_d.Add(p);

                if (GameStep % STEP_GROUP_SIZE == 0)
                {
                    if (rewardGroups.Count() > 0)
                        rewardGroups[rewardGroups.Count() - 1].capturedEbots = 
                            CapturedEvaders - rewardGroups[rewardGroups.Count() - 1].capturedEbots; // keep how many evaders were captured to gain the reward
                    rewardGroups.Add(new RewardGroup(CapturedEvaders));
                }
                rewardGroups[rewardGroups.Count()-1].reward += checkConnectedEvaders();
                //transferredData += checkConnectedEvaders();

                float remainingCredit = evadersCredit;
                remainingCredit = Math.Min(remainingCredit, (float)(Params.A_E.Count - nextEvaderIdxToAllocate));
                
                Pi_e.setGameState(MostUpdatedEvadersRound, O_d,EvaderLocations, remainingCredit, LatestPursuerLocations);
                this.CapturedEvaders += O_d.Count;
                //O_d = new HashSet<Utils.CapturedObservation>();
                pursuerTurn = false;
            } 
            else
            {

                O_c = Pi_e.communicate();

                //LatestEvaderLocations = Pi_e.getNextStep();
                //evadersCredit -= LatestEvaderLocations.Count;
                //if (evadersCredit < 0)
                //    throw new Exception("Evader policy allocated too many evaders");
                //foreach (var p in LatestEvaderLocations)
                //    AllEvaderLocations.Add(p);
                //++MostUpdatedEvadersRound;
                //evadersCredit += Params.f_e;
                //foreach (var e in LatestEvaderLocations)
                //    EvaderLocations[e] = Params.A_E[nextEvaderIdxToAllocate++];

                var LatestEvaderLocations = Pi_e.getNextStep();


                evadersCredit -= LatestEvaderLocations.Count - AllEvaderLocations.Count;
                if (evadersCredit < 0)
                    throw new Exception("Evader policy allocated too many evaders");
                AllEvaderLocations.Clear();
                foreach (var p in LatestEvaderLocations)
                    AllEvaderLocations.Add(p);
                ++MostUpdatedEvadersRound;
                evadersCredit += Params.f_e;
                EvaderLocations = new AgentGrid<Evader>(new Rectangle(0, 0, (int)graph.WidthCellCount, (int)graph.HeightCellCount));
                nextEvaderIdxToAllocate = 0;
                foreach (var e in LatestEvaderLocations)
                    EvaderLocations[e] = Params.A_E[nextEvaderIdxToAllocate++];



                // increase utility if evaders have a connected path
                // the algorithm below should be faster for smaller grids, but was rather delicate and wasn't tested yet
                //List<bool> prevColumnConnectedEvaders = EvaderLocations.getColumn(0);
                //for(int x = 1; prevColumnConnectedEvaders.Count > 0 && x < graph.WidthCellCount; ++x)
                //{
                //    var currentColumn = EvaderLocations.getColumn(x);
                //    List<bool> nextConnected = new List<bool>();
                //    for (int y = 0; y < graph.HeightCellCount; ++y)
                //    {
                //        int connectedGroupStart = y;
                //        while (currentColumn[y] && !prevColumnConnectedEvaders[y])
                //            ++y;
                //        if (currentColumn[y] == false)
                //            continue; // none in the group of evaders in column x was connected to any evader in column x-1
                //        // if both currentColumn[y] and prevColumnConnectedEvaders[y] are true, then all evaders in column x between 'connectedGroupStart' and the first unoccupied node are connected
                //        int connectedY = connectedGroupStart;
                //        for (;connectedY < graph.HeightCellCount && currentColumn[connectedY];
                //            ++connectedY)
                //        {
                //            nextConnected[connectedY] = true;
                //        }
                //        y = connectedY;
                //    }

                //    prevColumnConnectedEvaders = currentColumn;
                //}


                AllTransmittingEvaders = O_c.getAllTransmittingPoints();
                AllTransmittingEvaders.Remove(CommunicationGraph.SourcePoint);
                DetectedEvaders = new List<Point>();
                // filter out from O_C undetected transmissions
                if (transmissionDetectionProb > 0)
                {
                    DetectedEvaders = new List<Point>();
                    foreach (var p in AllTransmittingEvaders)
                        if (rand.NextDouble() <= transmissionDetectionProb)
                            DetectedEvaders.Add(p);
                }
                
                Pi_p.setGameState(MostUpdatedPursuersRound, DetectedEvaders, O_d);

                pursuerTurn = true;
                ++GameStep;
            }

            if (Params.A_E.Count - CapturedEvaders <= graph.Nodes.Count)
            {
                finishGame();
                return false;
            }
            return true;
            
        }

        /// <summary>
        /// returns 1 if EvaderLocations contains a connected path from column 0 to column 'graph.Width'
        /// returns 0 otherwise
        /// </summary>
        /// <returns></returns>
        private double checkConnectedEvaders()
        {
            if (Params.r_e == 1)
            {
                bool[,] visited = new bool[graph.WidthCellCount, graph.HeightCellCount];
                for (int y = 0; y < graph.HeightCellCount; ++y)
                    if (EvaderLocations[0, y] != null)
                        if (checkConnectedEvadersRec(visited, 1, y))
                            return 1;
                return 0;
            }
            else
            {
                bool[,] processed = new bool[graph.WidthCellCount, graph.HeightCellCount];
                foreach (var d in O_d)
                    processed[d.where.X, d.where.Y] = true; // we can't process dead nodes
                List<Point> processing = new List<Point>(O_c.ConnectedPointsCount);
                processing.Add(CommunicationGraph.SourcePoint);
                
                while(processing.Count > 0)
                {
                    Point p = processing.Last();
                    processing.RemoveAt(processing.Count-1);
                    if (p == CommunicationGraph.SourcePoint || !processed[p.X, p.Y])
                    {
                        if(p != CommunicationGraph.SourcePoint)
                            processed[p.X, p.Y] = true;

                        var next = O_c.getForwardEdges(p);
                        if (next == CommunicationGraph.Sink)
                            return 1;
                        
                        #if DEBUG
                        foreach(var n in next)
                            if(p == CommunicationGraph.SourcePoint)
                            {
                                if(n.X > Params.r_e)
                                    throw new Exception("CommunicationGraph connected to source point with distance >r_e!");
                            }
                            else if( n.manDist(p) > Params.r_e)
                            {
                                throw new Exception("CommunicationGraph between nodes with distance >r_e!");
                            }
                        #endif
                        processing.AddRange(next);
                    }
                }
                return 0;
            }
        }
        /// <summary>
        /// spreads to unvisited greater x values and adjacent y values
        /// </summary>
        private bool checkConnectedEvadersRec(bool[,] visited, int x, int y)
        {
            if (y < 0 || y >= graph.HeightCellCount)
                return false;

            if (x == graph.WidthCellCount)
                return true;

            if (visited[x, y])
                return false;

            visited[x, y] = true;
            return EvaderLocations[x, y] != null && (
                checkConnectedEvadersRec(visited, x + 1, y) ||
                checkConnectedEvadersRec(visited, x, y - 1) ||
                checkConnectedEvadersRec(visited, x, y + 1));
        }

        /// <summary>
        /// set to true when finishGame() is called
        /// </summary>
        public bool IsFinished { get; protected set; }

        
        /// <summary>
        /// automatically called if on getNextStep() enough evaders are captured (and they can't get more utility)
        /// may be force called if the game ends abruptly (e.g. for performance estimation)
        /// </summary>
        public void finishGame()
        {
            int consideredCapturedEvaders = 0;
            double considerdReward = 0;

            int consideredSteps = 0;

            // fixme remove below
            //List<double> ratios = new List<double>();
            //List<double> totalRatios = new List<double>();
            //double totalCaptured = 0, totalReward = 0;
            //for (int i = rewardGroups.Count() - 1; i >= 0;)
            //{
            //    double s1 = 0, s2 = 0;
            //    int j;
            //    for(j = i; j > i-100 && j >= 0; --j)
            //    {
            //        s1 += rewardGroups[j].capturedEbots;
            //        s2 += rewardGroups[j].reward;


            //    }
            //    totalCaptured += s1;
            //    totalReward += s2;
            //    totalRatios.Add(totalReward / totalCaptured);
            //    ratios.Add(s2 / s1);
            //    i = j;
            //}
            if (Params.RewardPortion > 0.999)
            {
                // make a normal ratio between all captured and all reward
                for (int i = 0; i < rewardGroups.Count(); ++i)
                {
                    consideredCapturedEvaders += rewardGroups[i].capturedEbots;
                    considerdReward += rewardGroups[i].reward;
                }
            }
            else // we exclude the last group since it's not full, and the initial groups according to RewardPortion
            {
                for (int i = rewardGroups.Count() - 2; i >= 0; --i)

                {
                    consideredCapturedEvaders += rewardGroups[i].capturedEbots;
                    considerdReward += rewardGroups[i].reward;
                    consideredSteps += STEP_GROUP_SIZE;
                    if (consideredSteps + STEP_GROUP_SIZE > Params.RewardPortion * GameStep)
                        break;
                }
            }
            if(transferredData > CapturedEvaders)
            {
                int a = 0; // fixme remove
            }

            transferredData = considerdReward;
            CapturedEvaders = consideredCapturedEvaders;
            IsFinished = true;
        }

        
        public FrontsGridRoutingGameParams Params { get { return gm; } }
        

        /// <summary>
        /// when the amount of captured evaders is great enough to insure evaders 
        /// can't gain any more utility (the minimum is graph's width) - the game ends
        /// </summary>
        public int CapturedEvaders
        {
            get; protected set;
        }
        
        public int MostUpdatedPursuersRound
        {
            get;
            set;
        }
        public int MostUpdatedEvadersRound
        {
            get;
            set;
        }

        public int currentRound
        {
            get
            {
                return MostUpdatedEvadersRound;
            }
        }

        public IDictionary<string, string> ResultValues
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// updates EvaderLocations, updates CapturedEvaders, and returns list of dead evaders
        /// </summary>
        /// <param name="visitedPursuerPoints"></param>
        /// <returns></returns>
        private List<Utils.CapturedObservation> checkDestroyedAgents(List<Point> visitedPursuerPoints)
        {
            var Od = new List<Utils.CapturedObservation>();

            foreach (Point p in visitedPursuerPoints)
                if (EvaderLocations[p] != null)
                {
                    Od.Add(new Utils.CapturedObservation { where = p, who = EvaderLocations[p] });
                    EvaderLocations[p] = null;
                    AllEvaderLocations.Remove(p);
                }

            return Od;
        }

        public IGameParams constructGameParams()
        {
            return new FrontsGridRoutingGameParams();
        }

        public string getGraphTypename()
        {
            return GoE.Utils.ReflectionUtils.GetObjStaticTypeName(graph);
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
