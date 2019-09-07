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

    public abstract class AIntrusionPursuersPolicy :  APursuersPolicy
    {
        
        public abstract bool init(AGameGraph G, IntrusionGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams);
        public override bool init(AGameGraph G, IGameParams prm, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams)
        {
            IntrusionGameParams concreateParam = (IntrusionGameParams)prm;
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
        public abstract void setGameState(int currentRound, List<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d);

        /// <summary>
        /// invoked after setGameState()
        /// </summary>
        /// <returns>
        /// visiting path for each pursuer (first point is in distance 1 from previous location, last point is
        ///in distance r_p)
        /// </returns>
        public abstract Dictionary<Pursuer, List<Point>> getNextStep();

        /// <summary>
        /// allows policy to flush additional data to the log, after the game is done
        /// </summary>
        public override void gameFinished() { }

        public override APolicyOptimizer constructTheoreticalOptimizer()
        {
            return null;
        }
        
        ///// <summary>
        ///// may be called after init()
        ///// </summary>
        ///// <returns></returns>
        //public virtual GameResult getMaxLeakedDataTheoreticalBound() { return null; }


        //public abstract List<ArgEntry> policyInputKeys();
    }
    public class IntrusionPursuersPolicyUI : AIntrusionPursuersPolicy
    {
        private GridGameGraph g;
        private IntrusionGameParams gm;
        private IPolicyGUIInputProvider pgui;

        private List<Point> prevO_c;
        private IEnumerable<GameLogic.Utils.CapturedObservation> prevO_d;
        private Dictionary<Pursuer, Point> currentPursuersLocations = new Dictionary<Pursuer, Point>();
        private Point intrusionCenterPoint;

       

        public override List<ArgEntry> policyInputKeys()
        {
            
                return new List<ArgEntry>();
            
        }

        public override bool init(AGameGraph G, IntrusionGameParams prm, IPolicyGUIInputProvider gui, Dictionary<string, string> policyInput)
        {

            this.g = (GridGameGraph)G;
            this.gm = prm;
            this.pgui = gui;
            //foreach (Pursuer p in gm.A_P)
              //  currentPursuersLocations[p] = null;
            intrusionCenterPoint = g.getNodesByType(NodeType.Target).First();
            return true;
        }
        public IntrusionPursuersPolicyUI() { }

        public override void setGameState(int currentRound, List<Point> O_c, IEnumerable<GameLogic.Utils.CapturedObservation> O_d)
        {
            prevO_c = O_c;
            prevO_d = O_d;
            
            Dictionary<string, List<Point>> markedLocations = new Dictionary<string, List<Point>>();
            markedLocations.Add("Detected Evaders(O_c)", O_c);
            markedLocations.Add("Prev. Killed Evaders (O_d)", O_d.Select(x=> x.where).ToList());
            pgui.markLocations(markedLocations.toPointFMarkings());

            InputRequest req = new InputRequest();

            foreach (Pursuer p in gm.A_P)
            {
                if (currentRound == 0)
                {
                    //req.addMovementOption(p, new Location(intrusionCenterPoint),
                      //  GameLogic.Utils.getAllPointsInArea(gm.r_e + 10).Select(x => new Location(x)).ToList());
                    req.addMovementOption(p, new Location(new Point(-1,-1)),
                        getPossibleNextLocations(new Location(Location.Type.Unset)));
                }
                else
                    req.addMovementOption(p, new Location(currentPursuersLocations[p]), 
                        getPossibleNextLocations(new Location(currentPursuersLocations[p])));
            }
            pgui.setInputRequest(req);
        }
        
        public override Dictionary<Pursuer, List<Point>> getNextStep()
        {
            Dictionary<Pursuer, List<Point>> res = new Dictionary<Pursuer, List<Point>>();
            foreach (Pursuer p in gm.A_P)
            {
                currentPursuersLocations[p] = pgui.getMovement(p).Last().nodeLocation;
                res[p] = pgui.getMovement(p).Select(x=>x.nodeLocation).ToList();
            }

            return res;
        }
        private List<Location> getPossibleNextLocations(Location currentPursuerLocation)
        {
            List<Location> res = new List<Location>();
            
            if (currentPursuerLocation.locationType == Location.Type.Unset)
                foreach (var n in g.Nodes.Keys)
                    res.Add(new Location(n));
            else
                foreach (var n in g.getNodesWithinDistance(currentPursuerLocation.nodeLocation, gm.r_p))
                    res.Add(new Location(n));

            return res;
        }
    }

   
}
