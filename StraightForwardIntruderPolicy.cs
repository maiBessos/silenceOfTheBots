using GoE.GameLogic;
using GoE.GameLogic.EvolutionaryStrategy;
using GoE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoE.Utils.Extensions;
using GoE.AppConstants;

namespace GoE.Policies
{
    /// <summary>
    /// intruders start evenly spread .
    /// all intruders simultenously (or with constant delay) go towards target, 
    /// and choose the points that don't collide with pursuers( maybe improve by maximizing the minimal distance to known pursuers)
    /// </summary>
    public class StraightForwardIntruderPolicy : AIntrusionEvadersPolicy
    {
        GridGameGraph gr;
        IntrusionGameState gs;
        IntrusionGameParams prm;
        Point intrusionCenter;
        int delayBetweenIntrusions;
        Random myrand;

        HashSet<Point> blockedPoints;

        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                res.Add(AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS);
                return res;
            }
        }
        public override void setGameState(int currentRound, IEnumerable<GameLogic.Utils.CapturedObservation> O_d, List<GameLogic.Utils.PursuerPathObservation> O_p, IntrusionGameState s)
        {
            gs = s;

            blockedPoints = new HashSet<Point>();
            foreach(var P in O_p)
                foreach(var p in P.observedPursuerPath)
                    blockedPoints.Add(p);
        }

        public override List<Evader> communicate()
        {
            return new List<Evader>();
        }

        Dictionary<Evader, int> remainingDelays = new Dictionary<Evader, int>();

        Dictionary<Evader, Point> res = new Dictionary<Evader, Point>();
        public override Dictionary<Evader, Point> getNextStep()
        {

            Dictionary<Evader, Point> nextRes = new Dictionary<Evader, Point>();
            if (gs.ActiveEvaders.Count == 0)
            {
                int d = 0;
                float ang = 0.1f + 0.8f * (float)myrand.NextDouble();
                foreach (Evader e in prm.A_E)
                {
                    nextRes[e] = intrusionCenter.add(GameLogic.Utils.getGridPointByAngle(prm.r_e + prm.r_es, ang));
                    ang += 4.0f / prm.A_E.Count;
                    remainingDelays[e] =d;
                    d += delayBetweenIntrusions;
                }
            }
            else
            {
                int i = 0;
                foreach (Evader e in gs.ActiveEvaders)
                {
                    if(remainingDelays[e] > 0)
                    {
                        --remainingDelays[e];
                        nextRes[e] = res[e];
                    }
                    else
                    {
                        if(res[e].manDist(intrusionCenter) == prm.r_e)
                            nextRes[e] = res[e]; // already started intruding
                        else
                        {
                            bool isBlocked;
                            nextRes[e] =
                                GameLogic.Utils.advanceOnPath(gr, res[e], intrusionCenter, myrand.Next(0,2)==0, blockedPoints, out isBlocked);
                        }
                    }

                }
            }
            res = nextRes;
            return nextRes;
        }

        //private List<string> argNames
        //{
        //    get
        //    {
        //        List<string> res = new List<string>();
        //        res.Add(AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS);
        //        return res;
        //    }
        //}
        //private List<string> defaultArgVals
        //{
        //    get
        //    {
        //        List<string> res = new List<string>();
        //        res.Add(AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS_DEFAULT);
        //        return res;
        //    }
        //}


        public override bool init(AGameGraph G, IntrusionGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            //myrand = new ThreadSafeRandom().rand;
            myrand = new Random((int)DateTime.Now.Ticks);
            this.gr = (GridGameGraph)G;
            intrusionCenter = gr.getNodesByType(NodeType.Target).First();
            this.prm = (IntrusionGameParams)prm;

            //if (policyParams == null)
            //    policyParams = new Dictionary<string, string>();

            //if (pgui.hasBoardGUI())
            //    policyParams.AddRange(argNames,pgui.ShowDialog(argNames.ToArray(), "StraightForwardIntruderPolicy init", null));

            //delayBetweenIntrusions = int.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS,
            //        AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS_DEFAULT));
            delayBetweenIntrusions = int.Parse(
                AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS.tryRead(policyParams));

            return true;
        }
    }
}