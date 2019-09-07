using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE;
using GoE.GameLogic;
using GoE.Policies;
using Utils;
using GoE.Utils.Algorithms;
using GoE.GameLogic.EvolutionaryStrategy;
using System.Drawing;

namespace GoE.Policies.Intrusion
{
    /// <summary>
    /// assumpes pursuers only patrol the circumference, 
    /// that the sensitive area is a square (1 edge between every two points on circumference)
    /// </summary>
    class PPDIntuder : AIntrusionEvadersPolicy
    {

        public override bool init(AGameGraph G, IntrusionGameParams Prm, 
            APursuersPolicy initializedPursuers, 
            UI.IPolicyGUIInputProvider Pgui, 
            Dictionary<string, string> policyParams = null)
        {
            this.g = (GridGameGraph)G;
            this.prm = (IntrusionGameParams)Prm;
            this.pgui = Pgui;
            this.activeStrategy = ((PPDPatrol)initializedPursuers).ActiveStrategy;


            //if (pgui.hasBoardGUI())
            //    policyParams.AddRange(argNames, pgui.ShowDialog(argNames.ToArray(), "StraightForwardIntruderPolicy init", null));

            //delayBetweenIntrusions = int.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS,
            //        AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS_DEFAULT));


            Point target = g.getNodesByType(NodeType.Target).First();
            //Point topRight = target.subtruct(prm.r_e / 2, prm.r_e / 2);
            sensitiveArea = prm.SensitiveAreaSquare(target); //new GameLogic.Utils.Grid4Square(topRight, prm.r_e-1);
            myRand = new ThreadSafeRandom().rand;
            return prm.IsAreaSquare;
        }
        
        public override void setGameState(int CurrentRound, 
                                          IEnumerable<GameLogic.Utils.CapturedObservation> O_d, 
                                          List<GameLogic.Utils.PursuerPathObservation> O_p, IntrusionGameState s)
        {
            this.currentRound = CurrentRound;
            this.o_p = O_p;
            this.o_d = O_d;
        }

        public override Dictionary<Evader, Point> getNextStep()
        {
            if(currentRound == 0)
            {
                foreach (Evader e in prm.A_E)
                {
                   // latestLocations[e] = sensitiveArea.Center.add(-1 - sensitiveArea.EdgeLenRad, 0);
                }
            }
            else
            {
                
            }

            return latestLocations;
        }

        public override List<Evader> communicate()
        {
            return new List<Evader>();
        }



        private GridGameGraph g;
        private IntrusionGameParams prm;
        private UI.IPolicyGUIInputProvider pgui;
        
        private int currentRound;
        private List<GameLogic.Utils.PursuerPathObservation> o_p;
        private IEnumerable<GameLogic.Utils.CapturedObservation> o_d;

        private PPDPatrol.StrategyType activeStrategy;
        private float continueForwardProb; // a.k.a. 'p' from intrusion paper
        private float changeSegSizeProb; // used for 'MiddlePursuerFlactuates' strategy. tells the prob of a pursuer changing the size of it's adjacent segments, if there just was a rotation
        private GameLogic.Utils.Grid4Square sensitiveArea;
        private Random myRand;
        private Dictionary<GameLogic.Evader, System.Drawing.Point> latestLocations;
    }
}
