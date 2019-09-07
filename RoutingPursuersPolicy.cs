using GoE.GameLogic;

using GoE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.AppConstants;
using GoE.Utils.Extensions;

namespace GoE.Policies
{

    public abstract class AFrontsGridRoutingPursuersPolicy : APursuersPolicy
    {

        
        public abstract bool init(AGameGraph G, FrontsGridRoutingGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams);
        public override bool init(AGameGraph G, IGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            FrontsGridRoutingGameParams concreateParam = (FrontsGridRoutingGameParams)prm;
            return init(G, concreateParam, pgui, policyParams);
        }


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
        public abstract void setGameState(int currentRound, IEnumerable<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d);

        /// <summary>
        /// invoked after setGameState()
        /// </summary>
        /// <returns>
        /// visiting path for each pursuer (first point is in distance 1 from previous location, last point is
        ///in distance r_p)
        /// </returns>
        public abstract List<Point> getNextStep();

        /// <summary>
        /// allows policy to flush additional data to the log, after the game is done
        /// </summary>
        public override void gameFinished() { }

        public override APolicyOptimizer constructTheoreticalOptimizer()
        {
            return null;
        }
        
        //public abstract List<ArgEntry> policyInputKeys { get; }
    }
    public class RoutingPursuersPolicyUI : AFrontsGridRoutingPursuersPolicy
    {
        private GridGameGraph g;
        private FrontsGridRoutingGameParams gm;
        private IPolicyGUIInputProvider pgui;
        private IEnumerable<Point> prevO_c;
        private IEnumerable<GameLogic.Utils.CapturedObservation> prevO_d;
        private List<Point> currentPursuersLocations = new List<Point>();
       
        public override List<ArgEntry> policyInputKeys()
        {
            //get
            //{
                return new List<ArgEntry>();
            //}
        }

        public override bool init(AGameGraph G, FrontsGridRoutingGameParams prm, IPolicyGUIInputProvider gui, Dictionary<string, string> policyInput)
        {

            this.g = (GridGameGraph)G;
            this.gm = prm;
            this.pgui = gui;
            
            return true;
        }
        public RoutingPursuersPolicyUI() { }

        public override void setGameState(int currentRound, IEnumerable<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {
            prevO_c = O_c;
            prevO_d = O_d;
            
            Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();
            markedLocations.Add("Detected Evaders(O_c)", O_c.ToList())  ;
            markedLocations.Add("Prev. Killed Evaders (O_d)", O_d.Select(x=> x.where).ToList());
            pgui.markLocations(markedLocations.toPointFMarkings());

            InputRequest req = new InputRequest();

            foreach (Pursuer p in gm.A_P)
            {
                if (currentRound == 0)
                {
                    //req.addMovementOption(p, new Location(RoutingCenterPoint),
                      //  GameLogic.Utils.getAllPointsInArea(gm.r_e + 10).Select(x => new Location(x)).ToList());
                    req.addMovementOption(p, new Location(new Point(-1,-1)), getPossibleNextLocations());
                }
            }
            pgui.setInputRequest(req);
        }
        
        public override List<Point> getNextStep()
        {
            currentPursuersLocations.Clear();
            foreach (Pursuer p in gm.A_P)
                currentPursuersLocations.Add(pgui.getMovement(p).Last().nodeLocation);
            return currentPursuersLocations;
        }

        private List<Location> getPossibleNextLocations()
        {
            List<Location> res = new List<Location>();
            foreach (var n in g.Nodes.Keys)
                res.Add(new Location(n));
            return res;
        }
    }
    
}
