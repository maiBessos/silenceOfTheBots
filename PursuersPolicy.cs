using GoE.GameLogic;

using GoE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GoE.Utils;
using GoE.AppConstants;
using GoE.Utils.Extensions;

namespace GoE.Policies
{
    public abstract class AGoEPursuersPolicy : APursuersPolicy
    {

        /// <summary>
        /// allows processing that comes before initialization of the policy. 
        /// Since preprocess output depends solely on the game graph, game params and input, it may be reused 
        /// for initialization of multiple policy objects
        /// </summary>
        /// <param name="G"></param>
        /// <param name="prm"></param>
        /// <returns></returns>
        //public virtual Dictionary<string, string> preProcess(GridGameGraph G, GoEGameParams prm, Dictionary<string, string> policyInput) { return new Dictionary<string, string>(); }
        
        ///// <summary>
        ///// adapts preProcess() from IPursuersPolicy interface
        ///// </summary>
        ///// <param name="G"></param>
        ///// <param name="prm"></param>
        ///// <param name="input"></param>
        ///// <returns></returns>
        //public Dictionary<string, string> preProcess(GridGameGraph G, IGameParams prm, Dictionary<string, string> policyInput)
        //{
        //    return preProcess(G, (GoEGameParams)prm, policyInput);
        //}

        /// <summary>
        /// invoked before each getNextStep() calls
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="O_c">
        /// Locations of transmissions done by the evaders in the previous round
        /// </param>
        /// <param name="O_d">
        /// Locations of evaders captured since the previous setGameState() call 
        /// (due to the previous movement of the pursuers, and due to evaders "suiciding" and going 
        /// into where a pursuer was)
        /// </param>
        public abstract void setGameState(int currentRound, List<Point> O_c, IEnumerable<Point> O_d);

        /// <summary>
        /// invoked after setGameState()
        /// </summary>
        /// <returns>
        /// new locations for each pursuer
        /// </returns>
        public abstract Dictionary<Pursuer, Location> getNextStep();

        /// <summary>
        /// allows policy to flush additional data to the log, after the game is done
        /// </summary>
        public override void gameFinished() { }

        /// <summary>
        /// called once after object is constructed
        /// </summary>
        /// <param name="G"></param>
        /// <param name="prm"></param>
        /// <param name="pgui"></param>
        /// <param name="PreprocessResult"> if null, init will call preProcess() </param>
        /// <returns>
        /// false if game parameters don't allow using this policy
        /// </returns>
        //public abstract bool init(AGameGraph G, IGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string,string> policyParams);
        //public abstract APolicyOptimizer constructTheoreticalOptimizer();

        //public abstract List<ArgEntry> policyInputKeys { get; }
    }
    public class PursuersPolicyUI : AGoEPursuersPolicy
    {
        private GridGameGraph g;
        private GoEGameParams gm;
        private IPolicyGUIInputProvider pgui;

        private List<Point> prevO_c;
        private IEnumerable<Point> prevO_d;
        private Dictionary<Pursuer, Location> currentPursuersLocations = new Dictionary<Pursuer, Location>();

        public override List<ArgEntry> policyInputKeys()
        {
            //get
           // {
                return new List<ArgEntry>();
           // }
        }

        public override bool init(AGameGraph G, IGameParams prm, IPolicyGUIInputProvider gui, Dictionary<string, string> policyInput)
        {
            this.g = (GridGameGraph)G;
            this.gm = (GoEGameParams )prm;
            this.pgui = gui;
            foreach (Pursuer p in gm.A_P)
                currentPursuersLocations[p] = new Location(Location.Type.Unset);
            return true;
        }
        public PursuersPolicyUI() { }

        public override void setGameState(int currentRound, List<Point> O_c, IEnumerable<Point> O_d)
        {
            prevO_c = O_c;
            prevO_d = O_d;

            Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();
            markedLocations.Add("Detected Evaders(O_c)", O_c);
            markedLocations.Add("Prev. Killed Evaders (O_d)", O_d.ToList());
            pgui.markLocations(markedLocations.toPointFMarkings());

            InputRequest req = new InputRequest();
            foreach(Pursuer p in gm.A_P)
                req.addMovementOption(p, currentPursuersLocations[p],getPossibleNextLocations(currentPursuersLocations[p]));

            pgui.setInputRequest(req);
        }
        
        public override Dictionary<Pursuer, Location> getNextStep()
        {
            foreach(Pursuer p in gm.A_P)
                currentPursuersLocations[p] = pgui.getMovement(p).First();
            
            return currentPursuersLocations;
        }
        private List<Location> getPossibleNextLocations(Location currentPursuerLocation)
        {
            List<Location> res = new List<Location>();
            res.Add(currentPursuerLocation);

            if (currentPursuerLocation.locationType == Location.Type.Unset)
                foreach (var n in g.Nodes.Keys)
                    res.Add(new Location(n));
            else
                foreach (var n in g.getNodesWithinDistance(currentPursuerLocation.nodeLocation, gm.r_p))
                    res.Add(new Location(n));

            return res;
        }

        public override APolicyOptimizer constructTheoreticalOptimizer()
        {
            return null;
        }
    }

	/*
    public class PursuersPolicySolution1 : IPursuersPolicy
    {
        private GridGameGraph g;
        private GameParams gm;
		private IPolicyInputProvider pgui;
        private Dictionary<Pursuer, Location> prevLocation = new Dictionary<Pursuer, Location>();
        Pursuer firstPursuer;
        private int currentRound;
		public void init(GridGameGraph G, GameParams prm, IPolicyInputProvider gui)
        {
            this.g = G;
            this.gm = prm;
            this.pgui = gui;
        }
>>>>>>> .r174

<<<<<<< .mine
=======
        public PursuersPolicySolution1()
        {
        }

        public void setGameState(int CurrentRound, List<Point> O_c, IEnumerable<Point> O_d)
        {
            this.currentRound = CurrentRound;
        }

        public Dictionary<Pursuer, Location> getNextStep()
        {
            if(currentRound == 0)
            {
                List<Point> targets = g.getNodesByType(NodeType.Target);
                int pursuerRange = 0;
                int pursuersPerTarget = gm.A_P.Count / targets.Count;
                foreach(Point t in targets)
                {
                    firstPursuer =
						Algorithms.InitUniformAreaPatrol(g, gm.r_p,
                            new Utils.ListRangeEnumerable<Pursuer>(gm.A_P, pursuerRange, pursuerRange + pursuersPerTarget),
                            prevLocation,
                            new Location(t), gm.r_e);
                        pursuerRange += pursuersPerTarget;
                }
            }
            else
            {
                
                List<Point> targets = g.getNodesByType(NodeType.Target);
                int pursuerRange = 0;
                int pursuersPerTarget = gm.A_P.Count / targets.Count;

                foreach(Point t in targets)
                {
					Algorithms.AdvanceUniformAreaPatrolPursuers(g, gm.r_p,
                        new Utils.ListRangeEnumerable<Pursuer>(gm.A_P, pursuerRange, pursuerRange + pursuersPerTarget),
                        prevLocation,
                        new Location(t), 
                        gm.r_e, 
                        firstPursuer);

                    pursuerRange += pursuersPerTarget;
                }
            }

            return prevLocation;
        }
    }*/

}
