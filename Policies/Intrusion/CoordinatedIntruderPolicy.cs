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
    public class CoordinatedIntruderPolicy : AIntrusionEvadersPolicy
    {
        GridGameGraph gr;
        IntrusionGameState gs;
        IntrusionGameParams prm;
        Point intrusionCenter;
        int delayBetweenIntrusions;
        Random myrand;
        bool alreadyCommunicated = false;
        HashSet<Point> blockedPoints;
        List<int> remainingDelays = new List<int>();

        public override List<ArgEntry> policyInputKeys
        {
            get
            {
                List<ArgEntry> res = new List<ArgEntry>();
                //res.Add(AppConstants.Policies.StraightForwardIntruderPolicy.DELAY_BETWEEN_INTRUSIONS.key);
                res.Add(AppConstants.Policies.CoordinatedIntruderPolicy.TIME_TO_LEARN);
                res.Add(AppConstants.Policies.CoordinatedIntruderPolicy.DELAY_BETWEEN_INTRUSIONS);
                return res;
            }
        }
        Evader communicatingEvader;
        List<GameLogic.Utils.PursuerPathObservation> Op;
        Dictionary<Evader, Point> res = new Dictionary<Evader, Point>();
        IPolicyGUIInputProvider ui;
        public override void setGameState(int currentRound, IEnumerable<GameLogic.Utils.CapturedObservation> O_d, 
                                          List<GameLogic.Utils.PursuerPathObservation> O_p, IntrusionGameState s)
        {
            gs = s;

            blockedPoints = new HashSet<Point>();
            foreach (var P in O_p)
                foreach (var p in P.observedPursuerPath)
                    blockedPoints.Add(p);

            Op = O_p;

            addGUIMarks(O_p, ui, s, gr, prm, res);
            
        }

        int roundsToLearn = 500;
        int maxObservedPursuers = 0;
        public override List<Evader> communicate()
        {
            var res = new List<Evader>();


            Dictionary<Evader, int> identifyingEvaders = new Dictionary<Evader, int>();

            foreach(var e in Op)
            {
                if (identifyingEvaders.ContainsKey(e.observer))
                    ++identifyingEvaders[e.observer];
                else
                    identifyingEvaders[e.observer] = 1;

                maxObservedPursuers = Math.Max(maxObservedPursuers, identifyingEvaders[e.observer]);

                if (roundsToLearn == 0 &&
                    identifyingEvaders[e.observer] >= maxObservedPursuers)
                {
                    res.Add(e.observer);
                }

                // if(e.observedPursuerPath.Count > prm.r_p)
                // {
                //    res.Add(e.observer);
                //    alreadyCommunicated = true;
                //    communicatingEvader = e.observer;
                //     return res;
                // }

                
                //for(int pi = 1; pi < e.observedPursuerPath.Count; ++pi)
                //{
                //    if(e.observedPursuerPath[pi-1].manDist(e.observedPursuerPath[pi])>1)
                //    {
                //        res.Add(e.observer);
                //        alreadyCommunicated = true;
                //        communicatingEvader = e.observer;
                //        return res;
                //    }
                //}
            }


            if (roundsToLearn > 0)
            {
                --roundsToLearn;
                return res;
            }

            if (res.Count >= 1)
            {
                // in this scenario, 1 evader tells the other to enter
                alreadyCommunicated = true;
                communicatingEvader = res.First();

                if (res.Count > 1)
                {
                    // 1 evader transmits, the other gets comm. before transmitting, then it enters without transmitting too
                    res.RemoveAt(res.Count - 1);
                }
            }
            
            return res;
        }


        public override Dictionary<Evader, Point> getNextStep()
        {
           
            Dictionary<Evader, Point> nextRes = new Dictionary<Evader, Point>();

            if (gs.ActiveEvaders.Count == 0)
            {
                int d = 0;
                float ang = 0.5f;
                foreach (Evader e in prm.A_E)
                {
                    nextRes[e] = intrusionCenter.add(GameLogic.Utils.getGridPointByAngle(prm.r_e + prm.r_es, ang));
                    ang += 4.0f / prm.A_E.Count;
                }
            }
            else
            {
                int i = 0;
                foreach (Evader e in gs.ActiveEvaders)
                {
                    
                    if (!alreadyCommunicated || res[e].manDist(intrusionCenter) == prm.r_e)
                        nextRes[e] = res[e]; // already started intruding
                    else
                    {
                        bool isBlocked;
                        nextRes[e] =
                            GameLogic.Utils.advanceOnPath(gr, res[e], intrusionCenter, myrand.Next(0, 2) == 0, blockedPoints, out isBlocked);
                    }
                    

                    ++i;
                }
            }
            res = nextRes;
            return nextRes;
        }
        public override bool init(AGameGraph G, IntrusionGameParams prm, APursuersPolicy initializedPursuers, IPolicyGUIInputProvider pgui, Dictionary<string, string> policyParams = null)
        {
            //myrand = new ThreadSafeRandom().rand;
            myrand = new Random((int)DateTime.Now.Ticks);
            this.gr = (GridGameGraph)G;
            intrusionCenter = gr.getNodesByType(NodeType.Target).First();
            this.prm = prm;



            //if (policyParams == null)
            //    policyParams = new Dictionary<string, string>();
            //if (pgui.hasBoardGUI())
            //    policyParams.AddRange(argNames, pgui.ShowDialog(argNames.ToArray(), "CoordinatedIntruderPolicy init", null));

            //delayBetweenIntrusions = int.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.CoordinatedIntruderPolicy.DELAY_BETWEEN_INTRUSIONS,
            //        AppConstants.Policies.CoordinatedIntruderPolicy.DELAY_BETWEEN_INTRUSIONS_DEFAULT));
            delayBetweenIntrusions = int.Parse(AppConstants.Policies.CoordinatedIntruderPolicy.DELAY_BETWEEN_INTRUSIONS.tryRead(policyParams));

            //roundsToLearn = int.Parse(
            //    Utils.ParsingUtils.readValueOrDefault(
            //        policyParams,
            //        AppConstants.Policies.CoordinatedIntruderPolicy.TIME_TO_LEARN,
            //        AppConstants.Policies.CoordinatedIntruderPolicy.TIME_TO_LEARN_DEFAULT));
            roundsToLearn = int.Parse(AppConstants.Policies.CoordinatedIntruderPolicy.TIME_TO_LEARN.tryRead(policyParams));


            ui = pgui;
            return true;
        }





    }
}