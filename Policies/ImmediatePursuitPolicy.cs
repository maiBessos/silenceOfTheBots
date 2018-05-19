//using GoE.GameLogic;
//using GoE.GameLogic.Algorithms;
//using GoE.GameLogic.EvolutionaryStrategy;
//using GoE.UI;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GoE.Policies
//{
//    public class ImmediatePursuitPolicy : APursuersPolicy
//    {
//        private GridGameGraph g;
//        private GameParams gm;
//        private IPolicyInputProvider pgui;
//        private Dictionary<Pursuer, Location> prevLocation = new
//            Dictionary<Pursuer, Location>();
//        private Dictionary<Pursuer, Location> initLocation = new
//            Dictionary<Pursuer, Location>();
//        Pursuer firstPursuer;
//        private int currentRound;
//        static private int k=1;
//        private List<Point> pursuitArea = new List<Point>();
//        Point pursuitTarget;
//        public override bool init(GridGameGraph G, GameParams prm, IPolicyInputProvider gui, Dictionary<string, string> PreprocessResult = null)
//        {
//            this.g = G;
//            this.gm = prm;
//            this.pgui = gui;
            
//            return true;
//        }

//        public ImmediatePursuitPolicy()
//        {
//        }

//        public override void setGameState(int CurrentRound, List<Point> O_c,
//            IEnumerable<Point> O_d)
//        {
//            this.currentRound = CurrentRound;
//            pursuitArea = new List<Point> ();
//            // TODO multiple pursuits in different cells?
//            if (O_c.Count > 0) {
//                Random rand = new ThreadSafeRandom().rand;
//                List<Point> possTargets = O_c.Where (key => g.getMinDistance (key, g.getNodesByType (NodeType.Target) [0]) <= gm.r_e).ToList();
//                if (possTargets.Count > 0) {
//                    int r = rand.Next (0, possTargets.Count);
//                    pursuitTarget = possTargets [r];
//                    g.getNodesWithinDistance (pursuitTarget, 1).ForEach (key => pursuitArea.Add (key));
//                }
//            }

//            Dictionary<string, List<Point>> markedLocations = new
//                Dictionary<string, List<Point>>();
//            //List<Location> pa = Algorithms.getPursuitArea (g, new	Location (pursuitTarget), pursuitRound);
//            if (pursuitArea.Count > 0) {
//                markedLocations.Add ("Evader spread", pursuitArea);
//                pgui.markLocations (markedLocations);
//            }

//        }

//        public override Dictionary<Pursuer, Location> getNextStep()
//        {
//            if(currentRound == 0)
//            {
//                List<Point> targets = g.getNodesByType(NodeType.Target);
//                int pursuerRange = 0;
//                int pursuersPerTarget = gm.A_P.Count / targets.Count;
//                foreach(Point t in targets)
//                {
//                    firstPursuer =
//                        Pursuit.InitImmediatePursuit(g, gm.r_p, k,
//                            new
//                            Utils.ListRangeEnumerable<Pursuer>(gm.A_P, pursuerRange, pursuerRange +
//                                pursuersPerTarget),
//                            prevLocation,
//                            new Location(t), gm.r_e);
//                    pursuerRange += pursuersPerTarget;
//                }
//                initLocation = new Dictionary<Pursuer,
//                Location>(prevLocation);
//            }
//            else
//            {	
//                    List<Point> targets = g.getNodesByType
//                        (NodeType.Target);
//                    int pursuerRange = 0;
//                    int pursuersPerTarget = gm.A_P.Count / targets.Count;

//                    foreach (Point t in targets) {
//                    Pursuit.AdvanceImmediatePursuit (g, gm.r_p, k,
//                            new Utils.ListRangeEnumerable<Pursuer> (gm.A_P, pursuerRange, pursuerRange + pursuersPerTarget),
//                            prevLocation,
//                            initLocation,
//                            pursuitTarget,
//                            pursuitArea,
//                            gm.r_e);

//                        pursuerRange += pursuersPerTarget;
//                    }
//                }

//            return prevLocation;
//        }

//        public static int neededPursuers (int r_e, int r_p, int k) 
//        {
//            return (2*k*(int)Math.Ceiling((double)(r_e+1)/r_p)^2);
//        }

//        public static double maxProb (int r_e, int r_p, int pursuers) {
//            int k = 5;
//            while (neededPursuers (r_e, r_p, k) > pursuers) {
//                k--;
//            }
//            return(k * 0.2);
//        }

//    }

//}

